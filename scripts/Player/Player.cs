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
	private CardData trumpCardData;
	private bool isDealerSelect = false;
	private int cardCountOfSuit = -1;

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

	private void OnTurnEndEvent()
	{

		isTurn = false;
		selectCards.Clear();
		cardCountOfSuit = -1;
	}

	private void OnTurnStartEvent(TurnData turnData, bool isDealer, CardData trumpCardData)
	{
		playerHandCard.SetAllCardIsSelected(false); // 先全部设置为不可选
		selectCards.Clear();
		this.turnData = turnData;
		this.trumpCardData = trumpCardData;
		isTurn = true;
		if (turnData.playType == PlayType.NONE)
		{
			playerHandCard.SetAllCardSelectable(true);
		}
		else
		{
			Suit suit = turnData.suit;
			cardCountOfSuit = playerHandCard.SetCardUnSelectableExpect(suit);
			// GD.Print($"当前花色{suit}有{cardCountOfSuit}张");
		}
	}

	private void OnSelectCardEvent(int id, bool isSelected)
	{
		// 更新选中列表
		if (isSelected)
			selectCards.Add(id);
		else
			selectCards.Remove(id);

		if (isTurn)
		{
			if (turnData.playType == PlayType.NONE)
				HandleBankerSelection();
			else
				HandleNonBankerSelection();
		}
		else if (isDealerSelect)
		{
			if (selectCards.Count == 8)
			{
				foreach (var card in playerHandCard.GetHandCards())
				{
					if (card.IsSelected)
						playerHandCard.SetCardSelectable(card, true); // 已选中的可取消
					else
						playerHandCard.SetCardSelectable(card, false);
				}
			}
			else
			{
				playerHandCard.SetAllCardSelectable(true);
			}
		}
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
		int selectCount = selectCards.Count;
		int requiredSuitCount = cardCountOfSuit; // 手牌中指定花色的总数
		int selectedSuitCount = selectCards.Count(id =>
		{
			CardData cd = CardData.Deserialize(id);
			Suit actualSuit = RuleEngine.GetSuit(cd, trumpCardData); // 需要访问 trumpCardData
			return actualSuit == turnData.suit;
		});

		if (selectCount == turnData.playNum)
		{
			// 达到要求数量：只允许取消已选中的牌
			foreach (var card in playerHandCard.GetHandCards())
			{
				playerHandCard.SetCardSelectable(card, card.IsSelected);
			}
		}
		else if (selectCount < turnData.playNum)
		{
			if (requiredSuitCount >= turnData.playNum)
			{
				// 牌足够，只能选指定花色
				playerHandCard.SetCardUnSelectableExpect(turnData.suit);
			}
			else
			{
				// 牌不够
				if (selectedSuitCount < requiredSuitCount)
				{
					// 指定花色还没选完，只能选指定花色（其他花色不可选）
					playerHandCard.SetCardUnSelectableExpect(turnData.suit);
				}
				else
				{
					// 指定花色已选完，可以选其他花色（所有剩余牌可选）
					foreach (var card in playerHandCard.GetHandCards())
					{
						// 所有牌都设为可选，包括已选中的（可取消）和未选的（可新增）
						playerHandCard.SetCardSelectable(card, true);
					}
				}
			}
		}
		else // selectCount > turnData.playNum (理论上不会发生，但为安全处理)
		{
			// 超过数量，只允许取消
			GD.PrintErr("选的牌超出数量了！");
			foreach (var card in playerHandCard.GetHandCards())
			{
				playerHandCard.SetCardSelectable(card, card.IsSelected);
			}
		}
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

	public void ExitrDeclareMode(bool isValid)
	{
		if (isValid)
			playerHandCard.SetAllCardSelectable(false, false);
		else
		{ playerHandCard.SetAllCardIsSelected(false); selectCards.Clear(); }
	}

	public void EnterSelectCard()
	{
		isDealerSelect = true;
		playerHandCard.SetAllCardIsSelected(false);
		selectCards.Clear();
		playerHandCard.SetAllCardSelectable(true);
	}

	public void ExitSelectCard(int[] ids)
	{
		isDealerSelect = false;
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
