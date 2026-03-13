using System.Collections.Generic;
using Euler.Event;
using Godot;

public partial class TurnRequest : Node2D
{
    private Player player;

    #region UI
    public void TurnStart(int seat, TurnData turnData)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcTurnStart), TurnData.Serialize(turnData));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcTurnStart(int id)
    {
        TurnEvent.OnTurnStartEvent(TurnData.Deserialize(id));
    }

    public void TurnEnd(int seat, bool isValid)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcTurnEnd), isValid);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcTurnEnd(bool isValid)
    {
        TurnEvent.OnTurnEndEvent(isValid);
    }
    #endregion
}