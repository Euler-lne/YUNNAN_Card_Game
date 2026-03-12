using Euler.Event;
using Godot;
using System;
using System.Collections.Generic;

public partial class PlayCardButton : Button
{
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
		}
	}

	private void TurnStartEvent(TurnData turnData)
	{
		Visible = true;
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
