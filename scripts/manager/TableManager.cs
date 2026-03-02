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

	public void ShowPlayerHand(int playerId, List<CardData> hand)
	{
		// GD.Print("Showing player hand " + hand.Count);
		seats[playerId].ShowHand(hand);
	}

	public void DealCard(int playerId, List<CardData> hand, CardData currentCard)
	{
		seats[playerId].DealCard(hand, currentCard);
	}
}
