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
		DealEvent.HandleHoleCardBeginEvent += OnHandleHoleCardBeginEvent;
	}

	public override void _ExitTree()
	{
		EventBus.PlayCardEvent -= OnPlayCard;
		DealEvent.HandleHoleCardBeginEvent -= OnHandleHoleCardBeginEvent;
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

	private void OnHandleHoleCardBeginEvent()
	{
		List<CardData> cardDatas = tableManager.RemoveAt(0); // 0是自己的位置
		for (int i = 1; i < GameSettings.PLAYER_COUNT; i++)
			tableManager.RemoveAt(i);
		playerHandCard.InsertCard(cardDatas);
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

	public void ExitrDeclareMode()
	{
		playerHandCard.SetAllCardSelectable(false, false);
	}

	public void EnterDealerSelectCard()
	{
		playerHandCard.SetAllCardSelectable(true);
		long peerId = Multiplayer.GetUniqueId();
		GD.Print($"{peerId}已经把所有牌设置为可以选");
	}
	public void ExitDealerSelectCard(int[] ids)
	{
		// 把选中的牌移除
		List<CardData> cardDatas = CardData.Deserialize(ids);
		playerHandCard.RemoveSeletedCard(cardDatas);
		playerHandCard.SetAllCardSelectable(false, false);
	}

	public void DealCard(CardData currentCard)
	{
		playerHandCard.InsertCard(currentCard);
	}

	public void RegenerateCardList(List<CardData> cardDatas, Rank rank, Suit suit)
	{
		playerHandCard.RegenerateCardList(cardDatas, rank, suit);
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
