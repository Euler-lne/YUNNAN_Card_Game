using Godot;
using Euler.Global;
using Euler.Event;
using System.Collections.Generic;


public partial class DealRequest : Node2D
{
    [Export] private Player player;
    [Export] private Card deckCard;
    [Export] private Node holdCardParent;
    private List<Card> holdCards = [];

    public void SetDealCard(bool visiable)
    {
        Rpc(nameof(RpcSetDealCard), visiable);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetDealCard(bool visiable)
    {
        deckCard.Visible = visiable;
    }
    #region UI相关
    public void UpdateTrumpSuit(Suit suit)
    {
        Rpc(nameof(RpcUpdateTrumpSuit), (int)suit);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcUpdateTrumpSuit(int suit)
    {
        UIEvent.OnChangeTrumpSuitEvent((Suit)suit);
    }
    public void SetTrumpSeatInvisiable()
    {
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
        {
            Rpc(nameof(RpcUpdateTrumpSeat), false, i);
        }
    }
    public void UpdateTrumpSeat(int seat)
    {
        // 传入seat设置为true，其余设置为false
        int count = GameSettings.PLAYER_COUNT;
        int temp = (seat + count / 2) % count;
        Rpc(nameof(RpcUpdateTrumpSeat), true, seat);
        Rpc(nameof(RpcUpdateTrumpSeat), true, temp);

        seat = (seat + 1) % count;
        temp = (seat + count / 2) % count;
        Rpc(nameof(RpcUpdateTrumpSeat), false, seat);
        Rpc(nameof(RpcUpdateTrumpSeat), false, temp);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcUpdateTrumpSeat(bool isTrump, int seat)
    {
        int logicalSeat = NetworkManager.Instance.GetViewSeat(seat);
        UIEvent.OnChangeTrumpEvent(isTrump, logicalSeat);
    }
    public void UpdateLastCardNum(int num)
    {
        Rpc(nameof(RpcUpdateLastCardNum), num);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcUpdateLastCardNum(int num)
    {
        UIEvent.OnChangeCardNumEvent(num);
    }
    #endregion

    #region 叫主相关
    public void SetDeclareUIInvisiable(long peerId)
    {
        RpcId(peerId, nameof(RpcSetDeclareUIInvisiable));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetDeclareUIInvisiable()
    {
        DealEvent.OnSetDeclareEvent(DeclareOption.NONE);
    }

    public void NotifyDeclareOption(long peerId, DeclareOption option)
    {
        RpcId(peerId, nameof(RpcNotifyDeclareOption), (int)option);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyDeclareOption(int optionInt)
    {
        DealEvent.OnSetDeclareEvent((DeclareOption)optionInt);
    }

    public void NotifyClientDeclareButtonPressed(long peerId, bool canDeclare)
    {
        RpcId(peerId, nameof(RpcNotifyClientDeclareButtonPressed), canDeclare);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyClientDeclareButtonPressed(bool isValid)
    {
        // 告知客户端点击叫主按钮是否合法
        DealEvent.OnJudgeDeclareRequestEvent(isValid);
    }

    public void NotifyClientConfirmButtonPressed(long peerId, bool isDeclareRight)
    {
        RpcId(peerId, nameof(RpcNotifyClientConfirmButtonPressed), isDeclareRight);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyClientConfirmButtonPressed(bool isValid)
    {
        // 告知客户端点击叫主按钮是否合法
        DealEvent.JudgeConfirmEvent(isValid);
        player.ExitrDeclareMode(); // 这里判断为点击按钮成功了
    }

    public void NotifyClientChooseTrump(long peerId, Rank rank)
    {
        RpcId(peerId, nameof(RpcNotifyClientChooseTrump), (int)rank);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyClientChooseTrump(int rank)
    {
        player.EnterDeclareMode((Rank)rank);  // 可以选了
    }
    #endregion

    #region 扣抵相关
    public void NotifyHostChooseMode(int hostSeat)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(hostSeat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcNotifyHostChooseMode));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyHostChooseMode()
    {
        // 显示UI，让对应的主选择遇大遇小
        DealEvent.OnChooseHoleEvent();
    }
    public void NotifyChooseModeResult(bool isBig)
    {
        Rpc(nameof(RpcNotifyChooseModeResult), isBig);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyChooseModeResult(bool isBig)
    {
        // 显示UI，让对应的主选择遇大遇小
        DealEvent.OnServerNotifyChooseHoleResultEvent(isBig);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void NotifyDealerGetRestCard(int dealer, int[] ids)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(dealer);
        if (peerId != -1)
        {
            foreach (var currentId in ids)
            {
                RpcId(peerId, nameof(RpcReceiveHand), currentId);
            }
            RpcId(peerId, nameof(RpcNotifyDealerSelectCard));
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyDealerSelectCard()
    {
        DealEvent.OnNotifyDealerSelectCard();
        player.EnterSelectCard(new());            // 通知可以选牌了
    }
    public void NotifyDealerSelectCardResult(bool isValid, int dealer, int[] ids)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(dealer);
        RpcId(peerId, nameof(RpcNotifyDealerSelectCardResult), isValid, ids);
        // deckCard.Visible = true;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNotifyDealerSelectCardResult(bool isValid, int[] ids)
    {
        DealEvent.OnNotifyDealerSelectCardResult(isValid);
        if (isValid)
            player.ExitSelectCard(ids);
    }
    public void HandleHoleCardBegin()
    {
        Rpc(nameof(RpcHandleHoleCardBegin));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcHandleHoleCardBegin()
    {
        DealEvent.OnHandleHoleCardBeginEvent();
    }

    public void RegenerateCardList(List<CardData>[] playerCardData, Rank rank, Suit suit)
    {
        if (!Multiplayer.IsServer()) return;
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
        {
            long peerId = NetworkManager.Instance.GetPeerIdBySeat(i);
            int[] ids = CardData.Serialize(playerCardData[i]);
            RpcId(peerId, nameof(RpcRegenerateCardList), ids, (int)rank, (int)suit);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcRegenerateCardList(int[] ids, int rank, int suit)
    {
        player.RegenerateCardList(CardData.Deserialize(ids), (Rank)rank, (Suit)suit);
    }

    #region 动画
    public void RotateToDealer(int dealerSeat)
    {
        Rpc(nameof(RpcRotateToDealer), dealerSeat);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcRotateToDealer(int dealerSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(dealerSeat);
        int mySeat = NetworkManager.Instance.MyLogicalSeat;
        if (viewSeat < mySeat)
            viewSeat += GameSettings.PLAYER_COUNT;
        var tween = CreateTween();
        tween.TweenProperty(deckCard, "rotation",
            deckCard.Rotation - Mathf.Pi / 2 * (viewSeat - mySeat),
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }
    public void GenerateHoleCard(Vector2 endPos, CardData cardData, bool isEnd = false)
    {
        Rpc(nameof(RpcGenerateHoleCard), endPos.X, endPos.Y, CardData.Serialize(cardData), isEnd);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcGenerateHoleCard(float endX, float endY, int cardData, bool isEnd)
    {
        Vector2 endPos = new(endX, endY);
        Card card = deckCard.Duplicate() as Card;
        card.Rotation = 0f;
        holdCardParent.AddChild(card);          // 添加到场景树 → 触发 _Ready
        holdCards.Add(card);
        card.SetCardData(CardData.Deserialize(cardData));
        if (isEnd) deckCard.Visible = false;
        var tween = CreateTween();
        tween.TweenProperty(
            card,
            "global_position",
            endPos,
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.TweenCallback(Callable.From(() =>
        {
            card.IsBack = false;
            // TODO：调用卡牌翻面函数
        }));
    }
    public void FlyCardToDealer(int dealerSeat)
    {
        Rpc(nameof(RpcFlyCardToDealer), dealerSeat);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcFlyCardToDealer(int dealerSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(dealerSeat);
        Vector2 targetPos = player.GetDealTargetPosition(viewSeat);
        var tween = CreateTween();
        tween.TweenProperty(
            deckCard,
            "global_position",
            targetPos,
            GameSettings.DEAL_DURATION_TIME / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.TweenCallback(Callable.From(() =>
        {
            deckCard.Visible = false;
        }));
    }
    public void GetherHoleCard()
    {
        Rpc(nameof(RpcGetherHoleCard));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcGetherHoleCard()
    {
        foreach (Card card in holdCards)
        {
            Vector2 screenSize = GetViewportRect().Size;
            Vector2 endPos = new(screenSize.X / 2, screenSize.Y / 2);
            card.IsBack = true; // TODO：调用卡牌翻面函数
            var tween = CreateTween();
            tween.TweenProperty(
                card,
                "global_position",
                endPos,
                GameSettings.DEAL_DURATION_TIME / 2)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);

            tween.TweenCallback(Callable.From(() =>
            {
                card.QueueFree();
            }));
        }
        holdCards.Clear();
        deckCard.Visible = true;
    }
    #endregion
    #region 工具函数
    public List<Vector2> GenerateHoleCardPosition()
    {
        // 获取屏幕尺寸
        Vector2 screenSize = GetViewportRect().Size;
        Vector2 screenCenter = new(screenSize.X / 2, screenSize.Y / 2);

        float cardWidth = CardParams.CARD_WIDTH;
        float cardHeight = CardParams.CARD_HEIGHT;

        float cardPosY = screenCenter.Y - cardHeight - CardLayoutParams.PUT_CARD_MARGIN;
        float offsetX = cardWidth + CardLayoutParams.PUT_CARD_MARGIN;
        float cardPosX = screenCenter.X - 3 * offsetX - offsetX / 2;
        List<Vector2> posList = [];
        for (int i = 0; i < 8; i++)
        {
            Vector2 pos = new(cardPosX, cardPosY);
            cardPosX += offsetX;
            posList.Add(pos);
        }
        return posList;
    }
    #endregion
    #endregion

    #region 发牌
    public void ReceiveHand(long peerId, int currentId)
    {
        RpcId(peerId, nameof(RpcReceiveHand), currentId);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcReceiveHand(int currentId)
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

    #region 发牌动画相关
    public void InitDeckRotation(int hostSeat)
    {
        Rpc(nameof(RpcInitDeckRotation), hostSeat);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcInitDeckRotation(int logicalSeat)
    {
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        deckCard.Rotation = Mathf.Pi / 2 * (1 - viewSeat);
    }

    public void EndDeckRotation(int hostSeat)
    {
        Rpc(nameof(RpcEndDeckRotation), hostSeat);
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

    public void RotateDeck()
    {
        Rpc(nameof(RpcRotateDeck));
    }
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

    public void FlyCard(int logicalSeat)
    {
        Rpc(nameof(RpcFlyCard), logicalSeat);
    }
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



    #endregion
}