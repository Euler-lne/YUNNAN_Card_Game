using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
using System.Threading.Tasks;

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
    }

    /// <summary>
    /// 服务器开始发牌
    /// </summary>
    public async void StartDeal()
    {
        if (!Multiplayer.IsServer() || GameCore == null)
            return;

        int total = GameSettings.PLAYER_COUNT * GameSettings.CARD_PRE_PLAYER;

        int hostSeat = 3; // TODO 真庄家
        Rpc(nameof(RpcInitDeckRotation), hostSeat);

        for (int i = 0; i < total; i++)
        {
            int logicalSeat = (i + hostSeat) % GameSettings.PLAYER_COUNT;

            await DealOneCardFlow(logicalSeat);
        }

        GD.Print("发牌结束");
        Rpc(nameof(RpcEndDeckRotation), hostSeat);
    }

    private async Task DealOneCardFlow(int logicalSeat)
    {
        // 1️⃣ 旋转动画（所有人执行）
        Rpc(nameof(RpcRotateDeck));

        await ToSignal(
            GetTree().CreateTimer(GameSettings.DEAL_DURATION_TIME / 2),
            SceneTreeTimer.SignalName.Timeout);

        // 2️⃣ 服务器本地发牌（只在服务器）
        var cardData = GameCore.DealOneCard(logicalSeat);

        int currentId = CardData.Serialize(cardData);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId == -1) // 没有这个人跳过
        { }
        else if (peerId == Multiplayer.GetUniqueId())  // 服务器发牌给自己
            ReceiveHand(logicalSeat, currentId);
        else
            NetworkManager.Instance.SendHand(peerId, logicalSeat, currentId);  // 服务器发牌给对应客户端

        // 3️⃣ 飞牌动画
        Rpc(nameof(RpcFlyCard), logicalSeat);

        await ToSignal(
            GetTree().CreateTimer(GameSettings.DEAL_DURATION_TIME / 2),
            SceneTreeTimer.SignalName.Timeout);
        if (peerId != -1)
        {
            DeclareOption option = GameCore.CheckIfSeatCanDeclare(logicalSeat);
            if (option != DeclareOption.NONE)
            {
                // 通知对应玩家客户端可以叫主
                RpcId(peerId, nameof(RpcNotifyDeclareOption), logicalSeat, (int)option);
            }
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyDeclareOption(int logicalSeat, int optionInt)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        DeclareOption option = (DeclareOption)optionInt;

        GD.Print($"{Multiplayer.GetUniqueId()}:我可以叫主，类型{option}");
    }

    /// <summary>
    /// 所有客户端调用显示牌
    /// </summary>
    public void ReceiveHand(int logicalSeat, int currentId)
    {
        if (tableManager == null)
        {
            GD.PrintErr("DealManager: tableManager 未初始化！");
            return;
        }
        CardData currentCard = CardData.Deserialize(currentId);
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        tableManager.DealCard(viewSeat, currentCard);
    }

    /// <summary>
    /// 初始化旋转方向
    /// </summary>
    /// <param name="logicalSeat"></param>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcInitDeckRotation(int logicalSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        deckCard.Rotation = Mathf.Pi / 2 * (1 - viewSeat);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcEndDeckRotation(int logicalSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        var tween = CreateTween();
        tween.TweenProperty(deckCard, "rotation",
            deckCard.Rotation - Mathf.Pi / 2 * ((viewSeat % 2) == 0 ? 1 : 0),
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// 旋转
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcRotateDeck()
    {
        var tween = CreateTween();
        tween.TweenProperty(deckCard, "rotation",
            deckCard.Rotation - Mathf.Pi / 2,
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }
    /// <summary>
    /// 飞牌
    /// </summary>
    /// <param name="logicalSeat"></param>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcFlyCard(int logicalSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        if (viewSeat == 0)
            return; // 是自己，不飞

        Card flyingCard = deckCard.Duplicate() as Card;
        AddChild(flyingCard);

        flyingCard.GlobalPosition = deckCard.GlobalPosition;
        flyingCard.Rotation = deckCard.Rotation;

        Vector2 targetPos = tableManager.GetDealTargetPosition(viewSeat);

        var tween = CreateTween();
        tween.TweenProperty(
            flyingCard,
            "global_position",
            targetPos,
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.TweenCallback(Callable.From(() =>
        {
            flyingCard.QueueFree();
        }));
    }

}