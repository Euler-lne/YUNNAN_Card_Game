using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
using Euler.Event;
using System.Linq;

public partial class Player : Node2D
{
	private PlayerHandCard playerHandCard;
	private TableManager tableManager;
	private List<int> selectCards = [];
	private TurnData turnData;
	private bool isTurn = false;

	public override void _Ready()
	{
		// FIXME:建议修改当前的UI布局
		playerHandCard = GetNode<PlayerHandCard>("../TableRoot/PlayerHandRoot");
		tableManager = GetNode<TableManager>("../TableRoot/TableManager");



		EventBus.PlayCardEvent += OnPlayCard;
		DealEvent.HandleHoleCardBeginEvent += OnHandleHoleCardBeginEvent;
		EventBus.SelectCardEvent += OnSelectCardEvent;
		EventBus.GetSelectCardEvent += OnGetSelectCardEvent;
		TurnEvent.TurnStartEvent += OnTurnStartEvent;
		TurnEvent.TurnEndEvent += OnTurnEndEvent;

		TurnEvent.CancelThrowCardEvent += OnCancelThrowCardEvent;
		TurnEvent.PlayCardEvent += OnPlayCardEvent;

	}

	public override void _ExitTree()
	{
		EventBus.PlayCardEvent -= OnPlayCard;
		DealEvent.HandleHoleCardBeginEvent -= OnHandleHoleCardBeginEvent;
		EventBus.SelectCardEvent -= OnSelectCardEvent;
		EventBus.GetSelectCardEvent -= OnGetSelectCardEvent;
		TurnEvent.TurnStartEvent -= OnTurnStartEvent;
		TurnEvent.TurnEndEvent -= OnTurnEndEvent;

		TurnEvent.CancelThrowCardEvent -= OnCancelThrowCardEvent;
		TurnEvent.PlayCardEvent -= OnPlayCardEvent;
	}

	private void OnPlayCardEvent()
	{
		playerHandCard.SetAllCardSelectable(false, false);
	}

	private void OnCancelThrowCardEvent()
	{
		playerHandCard.SetAllCardIsSelected(false);
		selectCards.Clear();
		playerHandCard.SetAllCardSelectable(true);
	}

	private void OnTurnEndEvent(bool isValid)
	{
		//TODO:玩家结束回合
		GD.Print($"Player得到是否合理{isValid}");
		if (isValid)
		{
			isTurn = false;
			selectCards.Clear();
		}
	}

	private void OnTurnStartEvent(TurnData turnData)
	{
		//TODO:玩家开始回合
		playerHandCard.SetAllCardIsSelected(false); // 先全部设置为不可选
		selectCards.Clear();
		this.turnData = turnData;
		isTurn = true;
		if (turnData.playType == PlayType.NONE)
		{
			playerHandCard.SetAllCardSelectable(true);
		}
		else
		{
			Suit suit = turnData.suit;
			playerHandCard.SetCardUnSelectableExpect(suit);
		}
	}

	private void OnSelectCardEvent(int id, bool isSelected)
	{
		// 更新选中列表
		if (isSelected)
			selectCards.Add(id);
		else
			selectCards.Remove(id);

		if (!isTurn) return;

		if (turnData.playType == PlayType.NONE)
			HandleBankerSelection();
		else
			HandleNonBankerSelection();
	}
	private void HandleBankerSelection()
	{
		var handCards = playerHandCard.GetHandCards();
		int selectCount = selectCards.Count;

		if (selectCount == 0)
		{
			// 无选中牌：所有牌可选
			playerHandCard.SetAllCardSelectable(true);
		}
		else
		{
			// 获取第一张选中的牌及其类别
			var firstCard = handCards.First(c => CardData.Serialize(c.cardData) == selectCards[0]);
			var firstCategory = playerHandCard.GetCardCategory(firstCard);

			foreach (var card in handCards)
			{
				if (card.IsSelected)
					playerHandCard.SetCardSelectable(card, true); // 已选中的可取消
				else
					playerHandCard.SetCardSelectable(card, playerHandCard.GetCardCategory(card) == firstCategory);
			}
		}
	}

	private void HandleNonBankerSelection()
	{
	}


	private List<int> OnGetSelectCardEvent()
	{
		return selectCards;
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

	public void EnterSelectCard()
	{
		playerHandCard.SetAllCardSelectable(true);
	}

	public void ExitSelectCard(int[] ids)
	{
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
