using Euler.Event;
using Godot;
using System;
using System.Collections.Generic;

public partial class PlayCardButton : Button
{
	private CardData trumpCardData = null;
	[Export] private ThrowCardUI throwCardInfo;
	private TurnData turnData;
	private bool isDealer;
	public override void _Ready()
	{
		Visible = false;

		Pressed += OnPlayCardButtonPressed;
		TurnEvent.TurnStartEvent += OnTurnStartEvent;
		TurnEvent.TurnEndEvent += OnTurnEndEvent;
		TurnEvent.SetTrumpCardDataEvent += OnSetTrumpCardDataEvent;
		TurnEvent.CancelThrowCardEvent += OnCancelThrowCardEvent;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPlayCardButtonPressed;
		TurnEvent.TurnStartEvent -= OnTurnStartEvent;
		TurnEvent.TurnEndEvent -= OnTurnEndEvent;
		TurnEvent.SetTrumpCardDataEvent -= OnSetTrumpCardDataEvent;
		TurnEvent.CancelThrowCardEvent -= OnCancelThrowCardEvent;

	}

	private void OnCancelThrowCardEvent()
	{
		Visible = true;
	}

	private void OnTurnEndEvent()
	{
		Visible = false;
	}

	private void OnTurnStartEvent(TurnData turnData, bool isDealer, CardData trumpCardData)
	{
		Visible = true;
		this.isDealer = isDealer;
		this.turnData = turnData;
	}

	private void OnSetTrumpCardDataEvent(CardData data)
	{
		trumpCardData = data;
	}

	private void OnPlayCardButtonPressed()
	{
		// 通知服务器

		// 判断当前选择卡牌的类型
		List<int> selectCards = EventBus.OnGetSelectCardEvent();

		if (selectCards.Count == 0)  // 选了0张牌直接返回
			return;
		TurnEvent.OnPlayCardEvent();
		PlayType playType = RuleEngine.DetermineSelectedPlayType(selectCards);
		if (turnData.playType == PlayType.NONE)
		{
			if (playType == PlayType.THROW_CARD)
			{
				throwCardInfo.PlayThrowCard(selectCards, isDealer);
				Visible = false;
				return;
			}
		}

		RpcId(1, nameof(RpcOnPlayCardButtonPressed), selectCards.ToArray());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcOnPlayCardButtonPressed(int[] ids)
	{
		TurnEvent.OnPlayCardButtonPressEvent(CardData.Deserialize(ids));
	}
}
