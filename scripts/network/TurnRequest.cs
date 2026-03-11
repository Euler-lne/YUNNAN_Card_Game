using Godot;

public partial class TurnRequest : Node2D
{
    private Player player;

    #region UI
    public void SetPlayUI(int seat, bool visiable)
    {
        long peerId = NetworkManager.Instance.GetPeerIdBySeat(seat);
        if (peerId == -1) return;
        RpcId(peerId, nameof(RpcSetPlayUI), seat, visiable);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetPlayUI(int seat, bool visiable)
    {

    }
    #endregion
}