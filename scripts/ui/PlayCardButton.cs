using Euler.Event;
using Godot;
using System;
using System.Collections.Generic;

public partial class PlayCardButton : Button
{
	[Export] Player player;
	List<CardData> selectedCard = [];
	public override void _Ready()
	{
		Visible = false;

		Pressed += OnPlayCardButtonPressed;
		TurnEvent.TurnStartEvent += TurnStartEvent;
		TurnEvent.TurnEndEvent += OnTurnEndEvent;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPlayCardButtonPressed;
		TurnEvent.TurnStartEvent -= TurnStartEvent;
		TurnEvent.TurnEndEvent -= OnTurnEndEvent;
	}

	private void OnTurnEndEvent(bool isValid, int[] ids)
	{
		if (isValid)
		{
			Visible = false;
			player.ExitSelectCard(ids);
			EventBus.SelectCardEvent -= OnSelectCardEvent;
		}
	}

	private void TurnStartEvent(TurnData turnData)
	{
		Visible = true;
		player.EnterSelectCard(turnData);
		EventBus.SelectCardEvent += OnSelectCardEvent;
	}
	private void OnSelectCardEvent(int[] ids)
	{
		selectedCard = CardData.Deserialize(ids);
	}

	private void OnPlayCardButtonPressed()
	{
		// 通知服务器
		// RpcId(1, nameof(RpcOnPlayCardButtonPressed), ids);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcOnPlayCardButtonPressed(int[] ids)
	{
		TurnEvent.OnPlayCardButtonPressEvent(CardData.Deserialize(ids));
	}
}
