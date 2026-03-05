using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;

public partial class Player : Node2D
{
	private PlayerHandCard playerHandCard;
	private TableManager tableManager;
	public override void _Ready()
	{
		// FIXME:建议修改当前的UI布局
		playerHandCard = GetNode<PlayerHandCard>("../TableRoot/PlayerHandRoot");
		tableManager = GetNode<TableManager>("../TableRoot/TableManager");



		NetworkManager.Instance.OnPlayCardEvent += OnPlayCard;
		NetworkManager.Instance.OnRemoveCardEvent += OnRemoveCard;
	}

	public override void _ExitTree()
	{
		NetworkManager.Instance.OnPlayCardEvent -= OnPlayCard;
		NetworkManager.Instance.OnRemoveCardEvent -= OnRemoveCard;
	}


	private void OnRemoveCard(int[] ids)
	{
		List<CardData> cardDatas = CardData.Deserialize(ids);
		playerHandCard.RemoveSeletedCard(cardDatas);
	}

	private void OnPlayCard(int playSeat, int[] ids, bool isBack, GamePhase gamePhase)
	{
		List<CardData> cardDatas = CardData.Deserialize(ids);
		tableManager.InsertCard(playSeat, cardDatas, isBack, gamePhase);
	}


	public void ExitrDeclareMode()
	{
		playerHandCard.SetAllCardSelectable(true);
	}

	public void EnterDeclareMode(Rank rank)
	{
		var cards = playerHandCard.GetHandCards();

		foreach (var card in cards)
		{
			bool canSelect = RuleEngine.CanSelect(card.cardData, GamePhase.DECLARE, rank);
			card.CanSelected = canSelect;
		}
	}

	public void DealCard(CardData currentCard)
	{
		playerHandCard.InsertCard(currentCard);
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
