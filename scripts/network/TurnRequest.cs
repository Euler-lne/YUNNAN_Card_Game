using System.Collections.Generic;
using Euler.Event;
using Godot;

public partial class TurnRequest : Node2D
{
    private Player player;

    #region UI
    public void TurnStart(int seat, TurnData turnData, bool isDealer)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcTurnStart), TurnData.Serialize(turnData), isDealer);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcTurnStart(int[] ids, bool isDealer)
    {
        TurnEvent.OnTurnStartEvent(TurnData.Deserialize(ids), isDealer);
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
    #endregion
}