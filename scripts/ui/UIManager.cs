using Godot;
using System;
using Euler.Event;

public partial class UIManager : Control
{
	private Button startButton;
	private Label currentPlayerNumber;
	private Label suitLabel;
	private Label leftCards;
	public DeclareContainer declareContainer;

	public override void _Ready()
	{
		startButton = GetNode<Button>("VBoxContainer/StartButton");
		currentPlayerNumber = GetNode<Label>("VBoxContainer/CurrentPlayerNumber");
		declareContainer = GetNode<DeclareContainer>("DeclareContainer");
		suitLabel = GetNode<Label>("SuitLabel");
		leftCards = GetNode<Label>("LeftCards");

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
		UIEvent.ChangeTrumpSuitEvent += OnChangeTrumpSuitEvent;
		UIEvent.ChangeCardNumEvent += OnChangeCardNumEvent;
	}

	public override void _ExitTree()
	{
		UIEvent.ChangeTrumpSuitEvent -= OnChangeTrumpSuitEvent;
		UIEvent.ChangeCardNumEvent -= OnChangeCardNumEvent;
	}

	private void OnChangeCardNumEvent(int leftCardNum)
	{
		leftCards.Text = leftCardNum.ToString();
	}

	private void OnChangeTrumpSuitEvent(Suit suit)
	{
		suitLabel.Text = suit switch
		{
			Suit.SPADE => "黑桃",
			Suit.HEART => "红心",
			Suit.CLUB => "梅花",
			Suit.DIAMOND => "方片",
			Suit.NONE => "无",
			_ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null)
		};
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