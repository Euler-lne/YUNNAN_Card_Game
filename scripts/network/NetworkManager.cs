using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;

public partial class NetworkManager : Node
{
	// 只负责RPC传输
	// 只负责座位管理
	public static NetworkManager Instance { get; private set; }
	public Dictionary<long, int> PeerToSeat { get; private set; } = [];
	// 保存着自己的逻辑座位号，不对应Seat的物理座位号，因为座位号会变。

	public Action<int> OnTotalPlayersChanged;

	private int _totalPlayers = 0;
	public int TotalPlayers
	{
		get
		{
			return _totalPlayers;
		}
		private set
		{
			_totalPlayers = value;
			if (Multiplayer.IsServer())
				OnTotalPlayersChanged?.Invoke(value);
		}
	}
	public int MyLogicalSeat { get; private set; } = -1;

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();

		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.PeerConnected += OnPeerConnected;
	}

	public void HostGame(int port)
	{
		var peer = new ENetMultiplayerPeer();
		peer.CreateServer(port, 4);
		Multiplayer.MultiplayerPeer = peer;

		GD.Print("Server started");
	}

	public void JoinGame(string ip, int port)
	{
		var peer = new ENetMultiplayerPeer();
		peer.CreateClient(ip, port);
		Multiplayer.MultiplayerPeer = peer;

		GD.Print("Client connecting...");
	}

	public void AssignServerSeat()
	{
		long serverId = Multiplayer.GetUniqueId();
		PeerToSeat[serverId] = 0;
		MyLogicalSeat = 0;
		TotalPlayers = 1;
	}

	public long GetPeerIdBySeat(int seat)
	{
		foreach (var pair in PeerToSeat)
		{
			if (pair.Value == seat)
				return pair.Key;
		}
		return -1;
	}

	private void OnPeerConnected(long id)
	{
		if (!Multiplayer.IsServer()) return;
		PeerToSeat[id] = TotalPlayers;
		RpcId(id, nameof(SetMySeat), TotalPlayers);

		TotalPlayers++;
		Rpc(nameof(SyncTotalPlayers), TotalPlayers);
		GD.Print("玩家加入: " + id + " 当前总人数: " + TotalPlayers);
	}

	private void OnPeerDisconnected(long id)
	{
		if (!Multiplayer.IsServer()) return;
		GD.Print("玩家离开: " + id);
		PeerToSeat.Remove(id);
		TotalPlayers--;
		Rpc(nameof(SyncTotalPlayers), TotalPlayers);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void SetMySeat(int seat) => MyLogicalSeat = seat;

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void SyncTotalPlayers(int count) => TotalPlayers = count;

	// 逻辑座位 -> 本地视角座位
	public int GetViewSeat(int logicalSeat)// logicalSeat 在自己作为中位于哪里，相对于自己
	{
		// 也就是对于B号玩家，如果发牌到了A号，那么把A的逻辑位置传递进来就可以得知它在B的视角下位于哪里
		return (logicalSeat - MyLogicalSeat + GameSettings.PLAYER_COUNT) % GameSettings.PLAYER_COUNT;
	}

	// 服务器发牌
	public void SendHand(long peerId, int logicalSeat, int[] cardIds, int currentId)
	{
		RpcId(peerId, nameof(ReceiveHand), logicalSeat, cardIds, currentId);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ReceiveHand(int logicalSeat, int[] cardIds, int currentId)
	{
		// 客户端接收手牌
		DealManager.Instance.ReceiveHand(logicalSeat, cardIds, currentId);
	}

	private void OnConnectedToServer()
	{
		GD.Print("连接成功");
		ChangeSceneManger.Instance.ChangeScene("uid://dh0r6wjeod2gf");
	}

	private void OnConnectionFailed()
	{
		GD.Print("连接失败");
	}
}
