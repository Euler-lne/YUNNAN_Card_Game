using Godot;
using Euler.Global;
using System.Threading.Tasks;
using System.Collections.Generic;
using Euler.Event;

/// <summary>
/// 发牌包括：
/// 1. 普通发牌（发牌堆旋转、从发牌堆生成牌并移动到手牌上）
/// 2. 发牌阶段需要记录当前的牌发到哪个位置，应为需要进行叫主、反主的操作
/// </summary>
public partial class DealManager : Node
{
    public GameCore GameCore { get; private set; }      // 只有服务器有


    private TaskCompletionSource<(DeclareOption, Suit)> declareTcs = null;
    private DealRequest dealRequest;
    private int activeDeclareSeat = -1;

    public override void _Ready()
    {
        dealRequest = GetNode<DealRequest>("DealRequest");
        DealEvent.CancelRequestEvent += OnCancelRequestEvent;
        DealEvent.ConfirmRequestEvent += OnConfirmRequestEvent;
        DealEvent.DeclareRequestEvent += OnDeclareRequestEvent;
    }
    public override void _EnterTree()
    {
        DealEvent.CancelRequestEvent -= OnCancelRequestEvent;
        DealEvent.ConfirmRequestEvent -= OnConfirmRequestEvent;
        DealEvent.DeclareRequestEvent -= OnDeclareRequestEvent;
    }


    public void Init(GameCore _gameCore)
    {
        GameCore = _gameCore;      // 服务器才有
    }

    public async void StartDeal()
    {
        if (!Multiplayer.IsServer())
            return;
        dealRequest.SetDealCard(true);
        int total = GameSettings.PLAYER_COUNT * GameSettings.CARD_PRE_PLAYER;

        int dealerSeat = GameCore.GetDealerSeat();
        dealRequest.InitDeckRotation(dealerSeat);

        for (int i = 0; i < total; i++)
        {
            int logicalSeat = (i + dealerSeat) % GameSettings.PLAYER_COUNT;

            await DealOneCardFlow(logicalSeat);
        }

        GD.Print("发牌结束");
        dealRequest.EndDeckRotation(dealerSeat);

        // TODO:如果没有定主给出1秒暂停，一秒之后还没有人叫主，那么翻开第一张牌判断当局的主
        // FIXME:鬼对是否可以反主？
    }

    private async Task DealOneCardFlow(int logicalSeat)
    {
        await WaitIfDeclaring();

        dealRequest.RotateDeck();
        await DelayHalf();

        var cardData = GameCore.DealOneCard(logicalSeat);
        int currentId = CardData.Serialize(cardData);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId != -1) // 发牌
            dealRequest.ReceiveHand(peerId, currentId);

        // 飞牌动画
        dealRequest.FlyCard(logicalSeat);
        await DelayHalf();

        CheckDeclare(logicalSeat);

    }
    #region 叫主相关
    private void CheckDeclare(int logicalSeat)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId == -1 || activeDeclareSeat != -1) return;

        DeclareOption option = GameCore.CheckIfSeatCanDeclare(logicalSeat);

        dealRequest.NotifyDeclareOption(peerId, option);
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
            dealRequest.SetDeclareUIInvisiable(peerId);
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

    private void OnDeclareRequestEvent(DeclareOption option, long peerId)
    {
        // 点击叫主
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];
        bool canDeclare = GameCore.CheckIfSeatCanDeclareOption(logicalSeat, option);
        Rank rank = GameCore.GetCurrentRank();
        dealRequest.NotifyClientDeclareButtonPressed(peerId, rank, canDeclare);
        if (!canDeclare) return;
        activeDeclareSeat = logicalSeat;

        declareTcs = new();

        SetDeclareUIInvisiable(logicalSeat);
    }
    public void OnConfirmRequestEvent(DeclareOption option, long peerId, int[] ids)
    {
        // 确定叫主
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];
        activeDeclareSeat = -1;
        List<CardData> cardDatas = CardData.Deserialize(ids);
        // GD.Print($"服务器收到客户端{peerId}选的牌");
        // foreach (CardData cardData in cardDatas)
        //     GD.Print($"花色{cardData.suit}，点数{cardData.suit}");
        // GD.Print($"结束");
        Rank rank = GameCore.GetCurrentRank();
        bool isDeclareRight = RuleEngine.IsDeclareRight(option, cardDatas, rank);
        dealRequest.NotifyClientConfirmButtonPressed(peerId, isDeclareRight);
        if (!isDeclareRight)
        {
            // TODO:可以通知客户端当前选择的牌不满足条件
            return;
        }

        // 无主的时候没有花色不知道花色
        Suit suit = option == DeclareOption.DARK_TRUMP ? Suit.NONE : cardDatas[0].suit;
        declareTcs?.SetResult((option, suit));
        declareTcs = null;

        // 将叫主的牌放到对应的牌库里面
        bool isBack = option == DeclareOption.DARK_TRUMP;
        GamePhase gamePhase = GameCore.GetCurrentGamePhase();
        NetworkManager.Instance.PlayCard(logicalSeat, cardDatas, isBack, gamePhase);
        GameCore.RemoveCardFrom(logicalSeat, cardDatas);

        GameCore.SetTrump(option, suit, logicalSeat);

        // 更新其他玩家 UI 更新UI使得不关闭也会有人自动关闭
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
            CheckDeclare(i);
    }

    public void OnCancelRequestEvent()
    {
        declareTcs.SetResult((DeclareOption.NONE, Suit.NONE));
        declareTcs = null;
    }

    #endregion


    // =============================
    // 工具函数
    // =============================
    private async Task DelayHalf()
    {
        await ToSignal(
            GetTree().CreateTimer(GameSettings.DEAL_DURATION_TIME / 2),
            SceneTreeTimer.SignalName.Timeout);
    }


}