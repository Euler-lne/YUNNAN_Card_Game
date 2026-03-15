using System.Collections.Generic;
using Euler.Event;
using Godot;

public partial class TurnRequest : Node2D
{
    private Player player;

    #region UI
    public void TurnStart(int seat, TurnData turnData, bool isDealer, CardData trumpCardData)
    {
        Rpc(nameof(RpcChangeCurrentPlayer), seat);
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcTurnStart), TurnData.Serialize(turnData), isDealer, CardData.Serialize(trumpCardData));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcTurnStart(int id, bool isDealer, int trumpId)
    {
        TurnEvent.OnTurnStartEvent(TurnData.Deserialize(id), isDealer, CardData.Deserialize(trumpId));
    }

    public void TurnEnd(int seat)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcTurnEnd));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcTurnEnd()
    {
        TurnEvent.OnTurnEndEvent();
    }

    public void SetInfo(int seat, string info)
    {
        if (seat == -1) Rpc(nameof(RpcSetInfo), info);
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcSetInfo), info);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetInfo(string info)
    {
        UIEvent.OnSetInfoEvent(info);
    }

    public void TurnOver()
    {
        Rpc(nameof(RpcChangeCurrentPlayer), -1);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcTurnOver()
    {

    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcChangeCurrentPlayer(int seat)
    {
        if (seat != -1)
            seat = NetworkManager.Instance.GetViewSeat(seat);
        UIEvent.OnChangeCurrentPlayerEvent(seat);
    }


    public void NewTurn(bool isDealer, int dealer)
    {
        int dealerSeat = NetworkManager.Instance.GetViewSeat(dealer);
        Rpc(nameof(RpcNewTurn), isDealer, dealerSeat);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNewTurn(bool isDealer, int dealerSeat)
    {
        TurnEvent.OnNewTurnEvent(isDealer, dealerSeat);
    }

    public void ChangeLevel(int seat, Rank rank)
    {
        Rpc(nameof(RpcChangeLevel), seat, (int)rank);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcChangeLevel(int seat, int rank)
    {
        int logicSeat = NetworkManager.Instance.GetViewSeat(seat);
        UIEvent.OnChangeLevelEvent(logicSeat, (Rank)rank);
    }
    #endregion

    #region 最后
    public void ExpandScoreCard(int len)
    {
        Rpc(nameof(RpcExpandScoreCard), len);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcExpandScoreCard(int len)
    {
        TurnEvent.OnExpandScoreCardEvent(len);
    }

    public void ExpandTableCard(List<CardData> tableCards)
    {
        Rpc(nameof(RpcExpandTableCard), CardData.Serialize(tableCards));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcExpandTableCard(int[] ids)
    {
        TurnEvent.OnExpandTableCardEvent(ids);
    }

    public void MoveCardToScore(CardData cardData)
    {
        Rpc(nameof(RpcMoveCardToScore), CardData.Serialize(cardData));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcMoveCardToScore(int id)
    {
        TurnEvent.OnMoveCardToScoreEvent(id);
    }
    public void ClearPointCards()
    {
        Rpc(nameof(RpcClearPointCards));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcClearPointCards()
    {
        TurnEvent.OnClearPointCardsEvent();
    }
    #endregion
}