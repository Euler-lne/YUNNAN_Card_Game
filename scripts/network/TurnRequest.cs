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


    public void NewTurn(bool isDealer)
    {
        Rpc(nameof(RpcNewTurn), isDealer);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcNewTurn(bool isDealer)
    {
        TurnEvent.OnNewTurnEvent(isDealer);
    }
    #endregion
}