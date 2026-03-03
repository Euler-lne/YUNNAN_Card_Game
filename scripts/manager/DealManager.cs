using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;

/// <summary>
/// 发牌包括：
/// 1. 普通发牌（发牌堆旋转、从发牌堆生成牌并移动到手牌上）
/// 2. 发牌阶段需要记录当前的牌发到哪个位置，应为需要进行叫主、反主的操作
/// </summary>
public partial class DealManager : Node
{
    public GameCore GameCore { get; private set; }      // 只有服务器有
    private TableManager tableManager;
    private Card deckCard;

    private List<CardData> localHands;

    private Queue<int> dealQueue;
    private Timer timer;

    public static DealManager Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
            QueueFree();
    }

    /// <summary>
    /// 初始化 DealManager
    /// 所有客户端都必须调用
    /// 服务器传入 GameCore，客户端传 null
    /// </summary>
    public void Init(GameCore _gameCore, TableManager _tableManager, Card _deckCard)
    {
        GameCore = _gameCore;      // 服务器才有
        tableManager = _tableManager;
        deckCard = _deckCard;


        // 客户端值初始化上面的信息就好了
        if (!Multiplayer.IsServer())
            return;

        dealQueue = new Queue<int>();
        timer = new Timer
        {
            WaitTime = GameSettings.DEAL_DURATION_TIME,
            OneShot = false,
            Autostart = false
        };
        AddChild(timer);
        timer.Timeout += OnDealTimeout;
    }

    /// <summary>
    /// 服务器开始发牌
    /// </summary>
    public void StartDeal()
    {
        if (!Multiplayer.IsServer() || GameCore == null)
            return; // 只有服务器执行发牌

        dealQueue.Clear();
        for (int i = 0; i < GameSettings.PLAYER_COUNT * GameSettings.CARD_PRE_PLAYER; i++)
            dealQueue.Enqueue(i % GameSettings.PLAYER_COUNT);

        timer.Start();
        int currentHost = 0; // TODO: 改成真正的庄家座位号
        Rpc(nameof(RpcDealAnimateRatate), currentHost, true);
    }

    private void OnDealTimeout()
    {
        // 客户端不会执行，因为Timer没有初始化
        if (dealQueue.Count == 0)
        {
            timer.Stop();
            GD.Print("发牌结束");
            return;
        }
        int logicalSeat = dealQueue.Dequeue();  // 当前要发给的逻辑座位号

        Rpc(nameof(RpcDealAnimateRatate), logicalSeat, false); // 所有角色转牌
    }

    /// <summary>
    /// 发牌动画（旋转牌堆）
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcDealAnimateRatate(int logicalSeat, bool isFirst)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        if (isFirst)
        {
            // 初始化牌堆方向
            deckCard.Rotation = -Mathf.Pi / 2 * viewSeat + Mathf.Pi / 2;
            return;
        }
        // Tween旋转牌堆，每次90度
        var tween = CreateTween();
        tween.TweenProperty(deckCard, "rotation",
            deckCard.Rotation - Mathf.Pi / 2,
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.TweenCallback(Callable.From(() =>
        {
            DealCard(logicalSeat);
        }));
    }

    private void DealCard(int logicalSeat)
    {
        // 旋转结束之后需要完成的操作
        var dealResult = GameCore.DealOneCard(logicalSeat);
        int[] ids = CardData.Serialize(dealResult.FullHand);
        int currentId = CardData.Serialize(dealResult.CurrentCard);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);

        if (peerId == -1)
        {
            // 玩家不存在，跳过
        }
        else if (peerId == Multiplayer.GetUniqueId())
        {
            // 自己发给自己，就是服务器自己发给自己
            ReceiveHand(logicalSeat, ids, currentId);
        }
        else
        {
            // 服务器向peerID发送牌
            NetworkManager.Instance.SendHand(peerId, logicalSeat, ids, currentId);
        }
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        GD.Print($"我是{Multiplayer.GetUniqueId()}，在我这里要给{viewSeat}发牌");
    }
    /// <summary>
    /// 所有客户端调用显示牌
    /// </summary>
    public void ReceiveHand(int logicalSeat, int[] ids, int currentId)
    {
        if (tableManager == null)
        {
            GD.PrintErr("DealManager: tableManager 未初始化！");
            return;
        }
        localHands = CardData.Deserialize(ids);
        CardData currentCard = CardData.Deserialize(currentId);
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        tableManager.DealCard(viewSeat, localHands, currentCard);
    }


    /// <summary>
    /// 发牌动画（旋转牌堆）
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcDealAnimateFly(int logicalSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        Card flyingCard = deckCard.Duplicate() as Card;
        AddChild(flyingCard);

        flyingCard.GlobalPosition = deckCard.GlobalPosition;
        flyingCard.Rotation = deckCard.Rotation;

        Vector2 targetPos = tableManager.GetDealTargetPosition(viewSeat);

        var moveTween = CreateTween();
        moveTween.TweenProperty(
            flyingCard,
            "global_position",
            targetPos,
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        moveTween.TweenCallback(Callable.From(() =>
        {
            flyingCard.QueueFree();
        }));
    }

}