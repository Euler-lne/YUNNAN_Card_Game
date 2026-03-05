using Godot;
using System;
using Euler.Event;

public partial class UIManager : Control
{
	private Button startButton;
	private Label currentPlayerNumber;
	public DeclareContainer declareContainer;

	public override void _Ready()
	{
		startButton = GetNode<Button>("VBoxContainer/StartButton");
		currentPlayerNumber = GetNode<Label>("VBoxContainer/CurrentPlayerNumber");
		declareContainer = GetNode<DeclareContainer>("DeclareContainer");

		declareContainer.Visible = false;

		if (Multiplayer.IsServer())
		{
			startButton.Visible = true;
			currentPlayerNumber.Visible = true;
		}
		else
		{
			startButton.Visible = false;
			currentPlayerNumber.Visible = false;
		}
	}



	#region 服务器开始游戏相关
	public void UpdatePlayerCount(int count)
	{
		currentPlayerNumber.Text = "当前人数: " + count;
	}

	public void ConnectStartButtonPressed(Action actions)
	{
		startButton.Pressed += () =>
		{
			startButton.Visible = false;
			currentPlayerNumber.Visible = false;
			actions?.Invoke();
		};
	}
	#endregion
}