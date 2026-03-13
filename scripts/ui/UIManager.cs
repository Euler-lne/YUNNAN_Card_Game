using Godot;
using System;
using Euler.Event;
using System.Collections.Generic;

public partial class UIManager : Control
{
	private Button startButton;
	private Label currentPlayerNumber;
	private Label suitLabel;
	private Label leftCards;
	private Label idlePlayerScore;
	private Button dealerGetCardConfirmButton;
	private DeclareContainer declareContainer;
	private VBoxContainer dealerGetCardUI;

	public override void _Ready()
	{
		startButton = GetNode<Button>("VBoxContainer/StartButton");
		currentPlayerNumber = GetNode<Label>("VBoxContainer/CurrentPlayerNumber");
		declareContainer = GetNode<DeclareContainer>("DeclareContainer");
		suitLabel = GetNode<Label>("SuitLabel");
		leftCards = GetNode<Label>("LeftCards");
		dealerGetCardConfirmButton = GetNode<Button>("DealerGetCardUI/Confirm");
		dealerGetCardUI = GetNode<VBoxContainer>("DealerGetCardUI");
		dealerGetCardUI.Visible = false;
		declareContainer.Visible = false;
		idlePlayerScore = GetNode<Label>("Score");
		idlePlayerScore.Text = "闲家分数：0";

		if (Multiplayer.IsServer())
		{
			startButton.Visible = true;
			currentPlayerNumber.Visible = true;
			EventBus.ChangeIdlePlayerScoreEvent += OnChangeIdlePlayerScoreEvent;
		}
		else
		{
			startButton.Visible = false;
			currentPlayerNumber.Visible = false;
		}
		UIEvent.ChangeTrumpSuitEvent += OnChangeTrumpSuitEvent;
		UIEvent.ChangeCardNumEvent += OnChangeCardNumEvent;
		DealEvent.NotifyDealerSelectCard += OnNotifyDealerSelectCard;
		DealEvent.NotifyDealerSelectCardResult += OnNotifyDealerSelectCardResult;
		dealerGetCardConfirmButton.Pressed += OnDealerGetCardConfirmButtonPressed;
	}

	public override void _ExitTree()
	{
		UIEvent.ChangeTrumpSuitEvent -= OnChangeTrumpSuitEvent;
		UIEvent.ChangeCardNumEvent -= OnChangeCardNumEvent;
		DealEvent.NotifyDealerSelectCard -= OnNotifyDealerSelectCard;
		DealEvent.NotifyDealerSelectCardResult -= OnNotifyDealerSelectCardResult;
		dealerGetCardConfirmButton.Pressed -= OnDealerGetCardConfirmButtonPressed;
		if (Multiplayer.IsServer())
		{ EventBus.ChangeIdlePlayerScoreEvent += OnChangeIdlePlayerScoreEvent; }
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

	#region 选底牌
	private void OnDealerGetCardConfirmButtonPressed()
	{
		RpcId(1, nameof(RpcOnDealerGetCardConfirmButtonPressed), EventBus.OnGetSelectCardEvent().ToArray());
	}
	private void OnNotifyDealerSelectCardResult(bool isValid)
	{
		if (isValid)
			dealerGetCardUI.Visible = false;
	}

	private void OnNotifyDealerSelectCard()
	{
		dealerGetCardUI.Visible = true;

	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcOnDealerGetCardConfirmButtonPressed(int[] ids)
	{
		DealEvent.OnDealerConfrimRequestEvent(ids);
	}
	#endregion

	#region 闲家分数
	private void OnChangeIdlePlayerScoreEvent(int value)
	{
		Rpc(nameof(RpcChangeIdlePlayerScoreEvent), value);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void RpcChangeIdlePlayerScoreEvent(int value)
	{
		idlePlayerScore.Text = $"闲家分数：{value}";
	}
	#endregion
}