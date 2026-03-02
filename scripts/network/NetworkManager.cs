using Godot;
using System;

public partial class NetworkManager : Node
{
	public static NetworkManager Instance { get; private set; }

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
	}

	public void HostGame(int port)
	{
		var peer = new ENetMultiplayerPeer();
		peer.CreateServer(port);
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
