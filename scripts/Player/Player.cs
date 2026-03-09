using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
using Euler.Event;

public partial class Player : Node2D
{
	private PlayerHandCard playerHandCard;
	private TableManager tableManager;
	public override void _Ready()
	{
		// FIXME:建议修改当前的UI布局
		playerHandCard = GetNode<PlayerHandCard>("../TableRoot/PlayerHandRoot");
		tableManager = GetNode<TableManager>("../TableRoot/TableManager");



		EventBus.PlayCardEvent += OnPlayCard;
	}

	public override void _ExitTree()
	{
		EventBus.PlayCardEvent -= OnPlayCard;
	}

	private void OnPlayCard(int playSeat, int[] ids, bool isBack, GamePhase gamePhase)
	{
		List<CardData> cardDatas = CardData.Deserialize(ids);
		tableManager.InsertCard(playSeat, cardDatas, isBack, gamePhase);
		if (playSeat == 0)
		{
			// 只有当前玩家删除，反正都是要删除，所以传递一次，在0号位置删除就好了
			playerHandCard.RemoveSeletedCard(cardDatas);
		}
	}


	public void ExitrDeclareMode()
	{
		var cards = playerHandCard.GetHandCards();
		foreach (var card in cards)
		{
			card.SetLight();
		}
	}

	public void EnterDeclareMode(Rank rank)
	{
		var cards = playerHandCard.GetHandCards();
		foreach (var card in cards)
		{
			bool canSelect = RuleEngine.CanSelect(card.cardData, GamePhase.DECLARE, rank);
			playerHandCard.SetCardSelectable(card, canSelect);
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
		else if (viewSeat == 0)
			return new(screenSize.X / 2, screenSize.Y);

		GD.PrintErr($"发牌动画不合理的发牌位置{viewSeat}");
		return Vector2.Zero;
	}

}
