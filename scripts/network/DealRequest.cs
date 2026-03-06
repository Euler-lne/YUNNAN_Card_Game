using Godot;
using Euler.Global;
using Euler.Event;


public partial class DealRequest : Node
{
    [Export] private Player player;
    [Export] private Card deckCard;

    public void SetDealCard(bool visiable)
    {
        Rpc(nameof(RpcSetDealCard), visiable);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetDealCard(bool visiable)
    {
        deckCard.Visible = visiable;
    }


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

    #region 动画相关
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