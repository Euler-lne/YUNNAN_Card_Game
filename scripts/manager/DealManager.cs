using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;

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
        Rpc(nameof(RpcDealAnimate), currentHost, true);
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
                                                // 每次发牌告诉所有客户端发给谁
        Rpc(nameof(RpcDealAnimate), logicalSeat, false);


        CardData currentCard = GameCore.DrawCardForPlayer(logicalSeat);
        List<CardData> cardDatas = GameCore.GetPlayerHand(logicalSeat);
        int[] ids = CardData.Serialize(cardDatas);
        int currentId = CardData.Serialize(currentCard);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);

        if (peerId == -1)
        {
            // 玩家不存在，跳过
        }
        else if (peerId == Multiplayer.GetUniqueId())
        {
            // 自己发给自己
            ReceiveHand(logicalSeat, ids, currentId);
        }
        else
        {
            // 服务器向peerID发送牌
            NetworkManager.Instance.SendHand(peerId, logicalSeat, ids, currentId);
        }
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
    private void RpcDealAnimate(int logicalSeat, bool isFirst)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);

        if (isFirst)
        {
            // 初始化牌堆方向
            deckCard.Rotation = Mathf.Pi / 2 * viewSeat;
        }
        else
        {
            // Tween旋转牌堆，每次90度
            var tween = CreateTween();
            tween.TweenProperty(deckCard, "rotation",
                deckCard.Rotation + Mathf.Pi / 2,
                GameSettings.DEAL_DURATION_TIME / 2)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }
    }

}