using Godot;
using System;

public partial class MainMenu : Panel
{
	private Button createGameButton;
	private Button joinGameButton;
	public override void _Ready()
	{
		createGameButton = GetNode<Button>("VBoxContainer/CreateGame");
		joinGameButton = GetNode<Button>("VBoxContainer/JoinGame");
		createGameButton.Pressed += OnPressCreateButton;
		joinGameButton.Pressed += OnPressJoinButton;
	}

	private void OnPressJoinButton()
	{
		NetworkManager.Instance.JoinGame("127.0.0.1", 7777);
	}

	private void OnPressCreateButton()
	{
		NetworkManager.Instance.HostGame(7777);
		ChangeSceneManger.Instance.ChangeScene("uid://dh0r6wjeod2gf");
	}
}
