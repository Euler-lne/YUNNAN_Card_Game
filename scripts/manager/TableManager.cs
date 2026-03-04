using Godot;
using System;
using System.Collections.Generic;
public partial class TableManager : Node2D
{
	private readonly Dictionary<int, Seat> seats = [];

	public override void _Ready()
	{
		seats[0] = GetNode<Seat>("../PlayerSeatRoot/SeatBottom");
		seats[1] = GetNode<Seat>("../PlayerSeatRoot/SeatRight");
		seats[2] = GetNode<Seat>("../PlayerSeatRoot/SeatTop");
		seats[3] = GetNode<Seat>("../PlayerSeatRoot/SeatLeft");
		// GD.Print("TableManager ready");
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

	public void DealCard(int playerId, CardData currentCard)
	{
		seats[playerId].InsertCard(currentCard);
	}

	public void EnterDeclareMode(int playerId, Rank rank)
	{
		var cards = seats[playerId].GetHandCards();

		foreach (var card in cards)
		{
			bool canSelect = RuleEngine.CanSelect(card.cardData, GamePhase.DECLARE, rank);
			card.CanSelected = canSelect;
		}
	}

	public void EnterDealMode(int playerId)
	{
		var cards = seats[playerId].GetHandCards();

		foreach (var card in cards)
		{
			card.CanSelected = false;
		}
	}
}
