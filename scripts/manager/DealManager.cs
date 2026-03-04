using Godot;
using Euler.Global;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 发牌包括：
/// 1. 普通发牌（发牌堆旋转、从发牌堆生成牌并移动到手牌上）
/// 2. 发牌阶段需要记录当前的牌发到哪个位置，应为需要进行叫主、反主的操作
/// </summary>
public partial class DealManager : Node
{
    public GameCore GameCore { get; private set; }      // 只有服务器有

    private Card deckCard;
    private Player player;
    private TaskCompletionSource<(DeclareOption, Suit)> declareTcs = null;

    private int activeDeclareSeat = -1;

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
    public void Init(GameCore _gameCore, Card _deckCard, Player _player)
    {
        GameCore = _gameCore;      // 服务器才有
        deckCard = _deckCard;
        player = _player;
    }

    /// <summary>
    /// 服务器开始发牌
    /// </summary>
    public async void StartDeal()
    {
        if (!Multiplayer.IsServer())
            return;

        int total = GameSettings.PLAYER_COUNT * GameSettings.CARD_PRE_PLAYER;

        int hostSeat = 0; // TODO: 真庄家
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
        await WaitIfDeclaring();

        Rpc(nameof(RpcRotateDeck));
        await DelayHalf();

        var cardData = GameCore.DealOneCard(logicalSeat);
        int currentId = CardData.Serialize(cardData);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId != -1) // 发牌
            RpcId(peerId, nameof(ReceiveHand), currentId);

        // 飞牌动画
        Rpc(nameof(RpcFlyCard), logicalSeat);
        await DelayHalf();

        // 检查是否可以叫主（仅显示按钮，不暂停发牌）

        CheckDeclare(logicalSeat);

    }
    #region 叫主相关
    private void CheckDeclare(int logicalSeat)
    {
        if (activeDeclareSeat != -1)  // 有人叫主期间不判断其他人是否可以叫主
            GD.Print($"当前{activeDeclareSeat}正在叫主，{logicalSeat}取消判断");
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId == -1 || activeDeclareSeat != -1) return;

        DeclareOption option = GameCore.CheckIfSeatCanDeclare(logicalSeat);
        // GD.Print($"{logicalSeat}检查叫主状态:{option}");
        RpcId(peerId, nameof(RpcNotifyDeclareOption), (int)option);
        if (option == DeclareOption.DARK_TRUMP)  // 等待判断
            declareTcs = new();
    }
    private void SetDeclareUIInvisiable(int expectLogicalSeat)
    {
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
        {
            if (i == expectLogicalSeat) continue;
            long peerId = NetworkManager.Instance.GetPeerIdBySeat(i);
            if (peerId == -1) continue;
            RpcId(peerId, nameof(RpcSetDeclareUIInvisiable));
        }
    }

    private async Task WaitIfDeclaring()
    {
        // =============================
        // 等待叫主结束
        // =============================
        if (declareTcs == null) return;
        await declareTcs?.Task;
        declareTcs = null;
        activeDeclareSeat = -1;
    }

    public void HandleDeclareRequest(DeclareOption option, long peerId)
    {
        // 点击叫主
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];
        bool canDeclare = GameCore.CheckIfSeatCanDeclareOption(logicalSeat, option);
        Rank rank = GameCore.GetCurrentRank();
        RpcId(peerId, nameof(RpcNotifyClientDeclareButtonPressed), (int)rank, canDeclare);
        if (!canDeclare) return;
        activeDeclareSeat = logicalSeat;
        GD.Print($"服务器再次判断，玩家 {logicalSeat} 可以叫主{option}");
        declareTcs = new();

        SetDeclareUIInvisiable(logicalSeat);
    }
    public void HandleConfirmDeclare(long peerId, int optionInt, int[] ids)
    {
        // TODO：需要告诉服务器选的是哪些牌
        // 确定叫主
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];
        activeDeclareSeat = -1;
        DeclareOption option = (DeclareOption)optionInt;
        List<CardData> cardDatas = CardData.Deserialize(ids);
        GD.Print($"服务器收到客户端{peerId}选的牌");
        foreach (CardData cardData in cardDatas)
            GD.Print($"花色{cardData.suit}，点数{cardData.suit}");
        GD.Print($"结束");
        Rank rank = GameCore.GetCurrentRank();
        bool isDeclareRight = RuleEngine.IsDeclareRight(option, cardDatas, rank);
        RpcId(peerId, nameof(RpcNotifyClientConfirmButtonPressed), isDeclareRight);
        if (!isDeclareRight)
        {
            // TODO:可以通知客户端当前选择的牌不满足条件
            return;
        }
        // 无主的时候没有花色不知道花色
        Suit suit = option == DeclareOption.DARK_TRUMP ? Suit.NONE : cardDatas[0].suit;
        declareTcs?.SetResult((option, suit));
        declareTcs = null;

        GameCore.SetTrump(option, suit, logicalSeat);
        GD.Print($"玩家 {logicalSeat} 确认叫主 {option} {suit}");
        GameCore.PrintDeclareInfo();

        // 更新其他玩家 UI 更新UI使得不关闭也会有人自动关闭
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
            CheckDeclare(i);
    }

    public void HandleCancelDarkDeclare()
    {
        GD.Print("取消暗主资格");

        declareTcs.SetResult((DeclareOption.NONE, Suit.NONE));
        declareTcs = null;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetDeclareUIInvisiable()
    {
        player.GetUIManager().Declare(DeclareOption.NONE);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyDeclareOption(int optionInt)
    {
        DeclareOption option = (DeclareOption)optionInt;
        player.GetUIManager().Declare(option);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcNotifyClientDeclareButtonPressed(int rank, bool isValid)
    {
        // 告知客户端点击叫主按钮是否合法
        player.GetUIManager().DeclareButtonPressed(isValid);
        player.EnterDeclareMode((Rank)rank);  // 可以选了
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcNotifyClientConfirmButtonPressed(bool isValid)
    {
        // 告知客户端点击叫主按钮是否合法
        player.GetUIManager().ConfirmButtonPressed(isValid);
        player.EnterDealMode();
    }
    #endregion


    #region 发牌
    /// <summary>
    /// 所有客户端调用显示牌
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ReceiveHand(int currentId)
    {
        if (player == null) // 会调用自己的player展示牌
        {
            GD.PrintErr("DealManager: player 未初始化！");
            return;
        }
        CardData currentCard = CardData.Deserialize(currentId);

        player.DealCard(currentCard);
    }
    #endregion
    #region 动画相关
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

        Vector2 targetPos = player.GetDealTargetPosition(viewSeat);

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
    // =============================
    // 工具函数
    // =============================
    private async Task DelayHalf()
    {
        await ToSignal(
            GetTree().CreateTimer(GameSettings.DEAL_DURATION_TIME / 2),
            SceneTreeTimer.SignalName.Timeout);
    }
    #endregion

}