using Godot;
using Euler.Global;
using System;

public partial class ClientRequestManager : Node
{
	public static ClientRequestManager Instance { get; private set; }

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();
	}

	/// <summary>
	/// 玩家点击 DeclareButton，告诉服务器我想叫主
	/// </summary>
	public void SendDeclareRequest(DeclareOption option)
	{
		long myPeerId = Multiplayer.GetUniqueId();
		// 调用服务器 RPC
		RpcId(1, nameof(ServerReceiveDeclareRequest), (int)option, myPeerId);
		// 假设服务器 id = 1
	}
	// RPC 由服务器实现
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ServerReceiveDeclareRequest(int option, long peerId)
	{
		// GD.Print($"服务器收到玩家 {peerId} 的叫主请求");
		// 这里服务器可以判断玩家是否真的有资格叫主，option是服务器想要的叫主类型
		// 如果有资格，DealManager.CreateTaskCompletionSource等
		DealManager.Instance.HandleDeclareRequest((DeclareOption)option, peerId);
	}

	/// <summary>
	/// 玩家点击 ConfirmButton，告诉服务器我确认了主花色/方式
	/// </summary>
	public void SendConfirmDeclare(DeclareOption option, Suit suit)
	{
		long myPeerId = Multiplayer.GetUniqueId();
		RpcId(1, nameof(ServerConfirmDeclare), myPeerId, (int)option, (int)suit);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ServerConfirmDeclare(long peerId, int optionInt, int suitInt)
	{
		// GD.Print($"服务器收到玩家 {peerId} 确认叫主，类型 {optionInt}, 花色 {suitInt}");
		DealManager.Instance.HandleConfirmDeclare(peerId, optionInt, suitInt);
	}

	public void SendCancelDarkDeclare()
	{
		RpcId(1, nameof(RpcSendCancelDarkDeclare));
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcSendCancelDarkDeclare()
	{
		DealManager.Instance.HandleCancelDarkDeclare();
	}


}