using Godot;
using System;
using System.Collections.Generic;

public partial class Player : Node2D
{
	private UIManager uiManager;
	private Seat seat;
	public override void _Ready()
	{
		uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
		seat = GetNode<Seat>("../TableRoot/PlayerSeatRoot/SeatBottom");
		uiManager.declareContainer.OnConfirmPressed += OnConfirmPressed;
	}

	private void OnConfirmPressed(DeclareOption option)
	{
		List<CardData> cardDatas = seat.GetSelectedCardList();
		int[] ids = CardData.Serialize(cardDatas);
		ClientRequestManager.Instance.SendConfirmDeclare(option, ids);
	}



	public UIManager GetUIManager() => uiManager;

	public void EnterDeclareMode(Rank rank)
	{
		var cards = seat.GetHandCards();

		foreach (var card in cards)
		{
			bool canSelect = RuleEngine.CanSelect(card.cardData, GamePhase.DECLARE, rank);
			card.CanSelected = canSelect;
		}
	}

	public void EnterDealMode()
	{
		seat.SetAllCardSelectable(false);
	}

	public void DealCard(CardData currentCard)
	{
		seat.InsertCard(currentCard);
	}

	public Vector2 GetDealTargetPosition(int viewSeat)
	{
		// 1 屏幕右 2 屏幕上 3 屏幕左
		Vector2 screenSize = GetViewportRect().Size;
		if (viewSeat == 1)
			return new(screenSize.X, screenSize.Y / 2);
		else if (viewSeat == 2)
			return new(screenSize.X / 2, 0);
		else if (viewSeat == 3)
			return new(0, screenSize.Y / 2);

		GD.PrintErr($"发牌动画不合理的发牌位置{viewSeat}");
		return Vector2.Zero;
	}

}
