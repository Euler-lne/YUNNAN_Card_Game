using Godot;
using Euler.Global;
using System.Threading.Tasks;
using System.Collections.Generic;
using Euler.Event;
using System;

/// <summary>
/// 发牌包括：
/// 1. 普通发牌（发牌堆旋转、从发牌堆生成牌并移动到手牌上）
/// 2. 发牌阶段需要记录当前的牌发到哪个位置，应为需要进行叫主、反主的操作
/// </summary>
public partial class DealManager : Node
{
    public GameCore GameCore { get; private set; }      // 只有服务器有


    private TaskCompletionSource<(DeclareOption, Suit)> declareTcs = null;
    private TaskCompletionSource<bool> handleholeTcs = null;

    private DealRequest dealRequest;
    private int activeDeclareSeat = -1;

    private Timer dealEndTimer = null;

    public override void _Ready()
    {
        dealRequest = GetNode<DealRequest>("DealRequest");
        DealEvent.CancelRequestEvent += OnCancelRequestEvent;
        DealEvent.ConfirmRequestEvent += OnConfirmRequestEvent;
        DealEvent.DeclareRequestEvent += OnDeclareRequestEvent;

        DealEvent.ClientNotifyChooseHoleResultEvent += OnClientNotifyChooseHoleResultEvent;
    }
    public override void _EnterTree()
    {
        DealEvent.CancelRequestEvent -= OnCancelRequestEvent;
        DealEvent.ConfirmRequestEvent -= OnConfirmRequestEvent;
        DealEvent.DeclareRequestEvent -= OnDeclareRequestEvent;
        DealEvent.ClientNotifyChooseHoleResultEvent -= OnClientNotifyChooseHoleResultEvent;
    }

    public void Init(GameCore _gameCore)
    {
        GameCore = _gameCore;      // 服务器才有
        if (Multiplayer.IsServer())
        {
            dealEndTimer = new Timer
            {
                WaitTime = GameSettings.DEAL_END_TIME,
                OneShot = true,
                Autostart = false
            };
            AddChild(dealEndTimer);
            dealEndTimer.Timeout += HandleHoleCard;

        }
    }
    public override void _ExitTree()
    {
        if (dealEndTimer != null)
            dealEndTimer.Timeout -= HandleHoleCard;
    }

    public async void StartDeal()
    {
        if (!Multiplayer.IsServer())
            return;
        dealRequest.SetDealCard(true);
        int total = GameSettings.PLAYER_COUNT * GameSettings.CARD_PRE_PLAYER;

        int dealerSeat = GameCore.GetDealerSeat();
        dealRequest.InitDeckRotation(dealerSeat);

        GD.Print($"本局发牌是否为抢庄{GameCore.IsSnatchDealer()}");

        for (int i = 0; i < 8; i++)
        {
            int logicalSeat = (i + dealerSeat) % GameSettings.PLAYER_COUNT;

            await DealOneCardFlow(logicalSeat);
        }

        GD.Print("发牌结束");
        dealRequest.EndDeckRotation(dealerSeat);
        dealEndTimer.Start();
    }
    #region 扣抵
    private async void HandleHoleCard()
    {
        // 处理底牌，计时器超时执行该函数
        await WaitIfDeclaring();
        SetDeclareUIInvisiable();
        if (!GameCore.HaveTrump())
        {
            HandleHoleCardNoTrump();
        }
        else
        {
            // 有主了锁住
            Suit suit = GameCore.GetTrumpSuit();
            GameCore.LockSuit(suit);
            GD.Print($"当前主为{suit}花色 庄家为{GameCore.GetDealerSeat()}号");
        }
        // TODO:如果为抢庄设置为抢庄结束
        // TODO:庄家拿牌，选牌
    }

    private void DealerGetCards()
    {
        // 牌给庄家
    }

    private async void HandleHoleCardNoTrump()
    {
        dealRequest.NotifyHostChooseMode(GameCore.GetDealerSeat());
        handleholeTcs = new();
        // 等待选择遇大遇小，并通知所有除了庄家的人当前选择的遇大还是遇小
        bool isBig = await handleholeTcs?.Task;  // 在点击了遇大/遇小释放
        dealRequest.NotifyChooseModeResult(isBig);

        await DelayHalf(GameSettings.INFO_EXIST_TIME);
        GD.Print("显示结束再继续向下走");

        // 开始从上到下依次展示牌，直到遇到主，没有主那么采用大/小
        List<Rank> ranks = [];
        if (GameCore.IsSnatchDealer() && !GameCore.IsSameRank())
        {
            // 抢庄且两者不一样，哪个队先遇到就是，哪个队
            ranks = GameCore.GetCurrentRank();
        }
        else
        {
            // 两家一样或者不是抢主
            int dealerSeat = GameCore.GetDealerSeat();
            Rank rank = GameCore.GetCurrentRank(dealerSeat);  // 得到当前庄家需要遇的牌
            ranks.Add(rank);
        }
        // CardData cardData = MeetTrump(ranks, isBig);
        // TODO: 翻开卡牌，然后得到当前是多少
        // GameCore.SetDealerSeat(dealer);
    }

    // private CardData MeetTrump(List<Rank> ranks, bool isBig)
    // {

    // }


    private void OnClientNotifyChooseHoleResultEvent(bool isBig)
    {
        handleholeTcs?.SetResult(isBig);
        handleholeTcs = null;
    }
    #endregion
    private async Task DealOneCardFlow(int logicalSeat)
    {
        dealRequest.RotateDeck();
        await DelayHalf(GameSettings.DEAL_DURATION_TIME / 2);

        var cardData = GameCore.DealOneCard(logicalSeat);  // 给对应玩家发牌，并返回插入的这张牌
        int currentId = CardData.Serialize(cardData);

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);
        if (peerId != -1) // 发牌
            dealRequest.ReceiveHand(peerId, currentId);

        // 飞牌动画
        dealRequest.FlyCard(logicalSeat);
        await DelayHalf(GameSettings.DEAL_DURATION_TIME / 2);

        CheckDeclare(logicalSeat);
        await WaitIfDeclaring();
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
    private void SetDeclareUIInvisiable(int expectLogicalSeat = -1)
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
        if (declareTcs == null) return;
        // 完整过程：点击亮主/反主，先判断是否合法，合法后在通知可以选牌
        // 选牌放在这里，防止漏牌，不合法也不会进入选牌
        if (activeDeclareSeat != -1)  //暗主没有设置，也不需要让玩家选派
        {
            Rank rank = GameCore.GetCurrentRank(activeDeclareSeat);
            long peerId = NetworkManager.Instance.GetPeerIdBySeat(activeDeclareSeat);
            if (peerId != -1)
                dealRequest.NotifyClientChooseTrump(peerId, rank);
        }

        await declareTcs?.Task;
        declareTcs = null;
        activeDeclareSeat = -1;
    }

    private void OnDeclareRequestEvent(DeclareOption option, long peerId)
    {
        // 点击叫主
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];
        bool canDeclare = GameCore.CheckIfSeatCanDeclareOption(logicalSeat, option);
        dealRequest.NotifyClientDeclareButtonPressed(peerId, canDeclare);
        if (!canDeclare) return;
        activeDeclareSeat = logicalSeat;

        declareTcs = new();

        SetDeclareUIInvisiable(logicalSeat);
    }
    public void OnConfirmRequestEvent(DeclareOption option, long peerId, int[] ids)
    {
        // 确定叫主
        int logicalSeat = NetworkManager.Instance.PeerToSeat[peerId];
        List<CardData> cardDatas = CardData.Deserialize(ids);
        // GD.Print($"服务器收到客户端{peerId}选的牌");
        // foreach (CardData cardData in cardDatas)
        //     GD.Print($"花色{cardData.suit}，点数{cardData.suit}");
        // GD.Print($"结束");
        Rank rank = GameCore.GetCurrentRank(logicalSeat);
        bool isDeclareRight = RuleEngine.IsDeclareRight(option, cardDatas, rank);
        dealRequest.NotifyClientConfirmButtonPressed(peerId, isDeclareRight);
        if (!isDeclareRight)
        {
            // TODO:可以通知客户端当前选择的牌不满足条件
            return;
        }

        Suit suit = cardDatas[0].suit;

        // 将叫主的牌放到对应的牌库里面
        bool isBack = option == DeclareOption.DARK_TRUMP;
        GamePhase gamePhase = GameCore.GetCurrentGamePhase();
        NetworkManager.Instance.PlayCard(logicalSeat, cardDatas, isBack, gamePhase);
        // 不能在服务器删除牌，因为这些牌并没有出

        GameCore.SetTrump(option, suit);

        // 如果为抢庄还需要设置当前的庄为现在的位置
        if (GameCore.IsSnatchDealer())
            GameCore.SetDealerSeat(logicalSeat);

        // 更新其他玩家 UI 更新UI使得不关闭也会有人自动关闭
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
            CheckDeclare(i);

        declareTcs?.SetResult((option, suit));
        declareTcs = null;
        activeDeclareSeat = -1;
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
    private async Task DelayHalf(float time)
    {
        await ToSignal(
            GetTree().CreateTimer(time),
            SceneTreeTimer.SignalName.Timeout);
    }


}