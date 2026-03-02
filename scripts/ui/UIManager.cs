using Godot;
using System;

public partial class UIManager : Control
{
	private Button startButton;
	private Label currentPlayerNumber;

	public override void _Ready()
	{
		startButton = GetNode<Button>("VBoxContainer/StartButton");
		currentPlayerNumber = GetNode<Label>("VBoxContainer/CurrentPlayerNumber");

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

	public void UpdatePlayerCount(int count)
	{
		currentPlayerNumber.Text = "Current Player: " + count;
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
}