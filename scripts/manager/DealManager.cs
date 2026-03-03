using Godot;
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
    private UIManager uiManager;
    private TaskCompletionSource<(DeclareOption, Suit)>[] declareTcsArr =
    new TaskCompletionSource<(DeclareOption, Suit)>[GameSettings.PLAYER_COUNT];
    private DeclareOption[] lastDeclareOption = new DeclareOption[GameSettings.PLAYER_COUNT];

    public static DealManager Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
            QueueFree();

        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
            lastDeclareOption[i] = DeclareOption.NONE;
    }

    /// <summary>
    /// 初始化 DealManager
    /// 所有客户端都必须调用
    /// 服务器传入 GameCore，客户端传 null
    /// </summary>
    public void Init(GameCore _gameCore, TableManager _tableManager, Card _deckCard, UIManager _uiManger)
    {
        GameCore = _gameCore;      // 服务器才有
        tableManager = _tableManager;
        deckCard = _deckCard;
        uiManager = _uiManger;

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
        Rpc(nameof(RpcRotateDeck));
        await ToSignal(GetTree().CreateTimer(GameSettings.DEAL_DURATION_TIME / 2), SceneTreeTimer.SignalName.Timeout);

        var cardData = GameCore.DealOneCard(logicalSeat);
        int currentId = CardData.Serialize(cardData);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId == -1) // 没有这个人跳过
        { }
        else if (peerId == Multiplayer.GetUniqueId())  // 服务器发牌给自己
            ReceiveHand(logicalSeat, currentId);
        else
            NetworkManager.Instance.SendHand(peerId, logicalSeat, currentId);  // 服务器发牌给对应客户端

        // 飞牌动画
        Rpc(nameof(RpcFlyCard), logicalSeat);

        await ToSignal(
            GetTree().CreateTimer(GameSettings.DEAL_DURATION_TIME / 2),
            SceneTreeTimer.SignalName.Timeout);

        // 1️⃣ 检查是否可以叫主（仅显示按钮，不暂停发牌）

        CheckDeclare(logicalSeat);

        // 2️⃣ 如果 declareTcs 存在（说明玩家点击了叫主），就暂停发牌
        if (declareTcsArr[logicalSeat] != null)
        {
            var result = await declareTcsArr[logicalSeat].Task;
            GameCore.SetTrump(result.Item1, result.Item2, logicalSeat);
            GD.Print($"玩家 {logicalSeat} 确认叫主 {result.Item1} {result.Item2}");
            declareTcsArr[logicalSeat] = null;
        }
    }
    #region 叫主相关
    private void CheckDeclare(int logicalSeat)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId == -1) return;

        DeclareOption option = GameCore.CheckIfSeatCanDeclare(logicalSeat);
        GD.Print($"名字{peerId}，状态当前{option}，之前状态{lastDeclareOption[logicalSeat]}");

        // 状态没有变化就不发送
        if (option != lastDeclareOption[logicalSeat])
        {
            lastDeclareOption[logicalSeat] = option;
            RpcId(peerId, nameof(RpcNotifyDeclareOption), (int)option);
        }
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


    /// <summary>
    /// 服务器收到玩家点击 declare
    /// </summary>
    public void HandleDeclareRequest(long peerId)
    {
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];

        DeclareOption canDeclare = GameCore.CheckIfSeatCanDeclare(logicalSeat);
        if (canDeclare == DeclareOption.NONE) return;

        GD.Print($"玩家 {logicalSeat} 可以叫主");

        declareTcsArr[logicalSeat] = new TaskCompletionSource<(DeclareOption, Suit)>();

        SetDeclareUIInvisiable(logicalSeat);
    }

    /// <summary>
    /// 服务器收到玩家点击 confirm
    /// </summary>
    public void HandleConfirmDeclare(long peerId, int optionInt, int suitInt)
    {
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];

        DeclareOption option = (DeclareOption)optionInt;
        Suit suit = (Suit)suitInt;

        declareTcsArr[logicalSeat]?.SetResult((option, suit));
        declareTcsArr[logicalSeat] = null;

        GameCore.SetTrump(option, suit, logicalSeat);
        GD.Print($"玩家 {logicalSeat} 确认叫主 {option} {suit}");

        // 更新其他玩家 UI
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
            CheckDeclare(i);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetDeclareUIInvisiable()
    {
        uiManager.Declare(DeclareOption.NONE);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyDeclareOption(int optionInt)
    {
        DeclareOption option = (DeclareOption)optionInt;
        uiManager.Declare(option);
        GD.Print($"{Multiplayer.GetUniqueId()}:我可以叫主，类型{option}");
    }
    #endregion


    #region 发牌
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
    #endregion

}