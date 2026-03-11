using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
using Euler.Event;

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

	private string[] nameList = [];
	private string[] avatarPathList = [];

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();
		nameList = GameSettings.GetRandomFourNames();
		avatarPathList = GameSettings.GetAvatarList();
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.PeerConnected += OnPeerConnected;
	}
	#region 出牌相关
	public void PlayCard(int playLogicSeat, List<CardData> cardDatas, bool isBack, GamePhase gamePhase)
	{
		if (!Multiplayer.IsServer()) return;
		int[] ids = CardData.Serialize(cardDatas);
		Rpc(nameof(RpcPlayCardBroadcast), playLogicSeat, ids, isBack, (int)gamePhase);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void RpcPlayCardBroadcast(int playLogicSeat, int[] ids, bool isBack, int _gamePhase)
	{
		int playSeat = NetworkManager.Instance.GetViewSeat(playLogicSeat); // 当前出牌的人在自己视角的逻辑座位
		GamePhase gamePhase = (GamePhase)_gamePhase;
		EventBus.OnPlayCardEvent(playSeat, ids, isBack, gamePhase);
	}
	#endregion


	#region 连接相关
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
		SetUI(serverId);
	}

	private void OnPeerConnected(long id)
	{
		if (!Multiplayer.IsServer()) return;
		PeerToSeat[id] = TotalPlayers;
		RpcId(id, nameof(SetMySeat), TotalPlayers);

		TotalPlayers++;
		SetUI(id);
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
	private void SetUI(long currentPeerId)
	{
		// 1. 已有玩家更新UI
		// 2. 通知新加入的玩家现有的UI
		int serverSeat = PeerToSeat[currentPeerId];
		Rpc(nameof(RpcSetUI), nameList[serverSeat], avatarPathList[serverSeat], serverSeat); //通知所有人 有人加入，并更新自己的UI

		foreach (var item in PeerToSeat)
		{
			// 给加入的这个人添加别人的UI
			if (item.Key == currentPeerId) continue;
			serverSeat = item.Value;
			RpcId(currentPeerId, nameof(RpcSetUI), nameList[serverSeat], avatarPathList[serverSeat], serverSeat);
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void RpcSetUI(string name, string path, int serverSeat)
	{
		int selfSeat = GetViewSeat(serverSeat);
		UIEvent.OnChangeNameEvent(name, selfSeat);
		UIEvent.OnChangeAvatarEvent(path, selfSeat);
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
	#endregion


	#region 工具函数
	public int GetViewSeat(int logicalSeat)// logicalSeat 在自己作为中位于哪里，相对于自己
	{
		// 也就是对于B号玩家，如果发牌到了A号，那么把A的逻辑位置传递进来就可以得知它在B的视角下位于哪里
		return (logicalSeat - MyLogicalSeat + GameSettings.PLAYER_COUNT) % GameSettings.PLAYER_COUNT;
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
	#endregion
}
