using Godot;
using System.Collections.Generic;
using Euler.Global;
using Euler.Event;
using System;
using System.Linq;

public partial class PlayerHandCard : Node2D
{
	[Export] public PackedScene cardScene;

	public bool isLocalPlayer = false;
	private List<Card> handCards = [];
	private List<Card> spadeCards = [];
	private List<Card> heartCards = [];
	private List<Card> clubCards = [];
	private List<Card> diamondCards = [];
	private List<Card> mainCards = [];
	private HashSet<int> spadeIdSet = [], heartIdSet = [], clubIdSet = [], diamondIdSet = [], mainIdSet = [];

	private List<Vector2> handPositions = [];
	private Card lastSelectCard = null;

	private bool isLeftMouseDown = false; // 左键是否按住

	private CardList handLogic = new();
	private float unselecteY = -1f;
	private float UnselecteY()
	{
		if (unselecteY < 0)
		{
			Vector2 screenSize = GetViewportRect().Size;
			float y = screenSize.Y + CardParams.CARD_HEIGHT / 2; // 手牌顶部位于屏幕底部
			y -= CardLayoutParams.BOTTOM_MARGIN_UNSELECT; // 当前手牌露出卡牌的0.75倍高度
			unselecteY = y;
		}
		return unselecteY;
	}

	public override void _Ready()
	{
		DealEvent.ConfirmDardTrumpEvent += OnConfirmDardTrumpEvent;

	}

	public override void _ExitTree()
	{
		DealEvent.ConfirmDardTrumpEvent -= OnConfirmDardTrumpEvent;

	}


	private CardData OnConfirmDardTrumpEvent()
	{
		return handLogic.cardList[0];
	}



	#region 计算手牌应该出现的位置
	public void GenerateHandPosition(int count)
	{
		handPositions.Clear();
		if (count == 0) return;

		// 获取屏幕尺寸
		Vector2 screenSize = GetViewportRect().Size;

		float cardWidth = CardParams.CARD_WIDTH;


		// 可用的最大总宽度
		float maxTotalWidth = screenSize.X - 2 * CardLayoutParams.HORIZONTAL_MARGIN;

		// 计算重叠系数：保证总宽度不超过 maxTotalWidth
		float overlap;
		if (count == 1)
		{
			overlap = 1.0f; // 单张卡牌不重叠
		}
		else
		{
			// 解方程：(hand.Count-1) * offset + cardWidth <= maxTotalWidth, 其中 offset = cardWidth * overlap
			float maxOffset = (maxTotalWidth - cardWidth) / (count - 1);
			overlap = Mathf.Min(1.0f, maxOffset / cardWidth); // 重叠系数不能大于1（允许重叠但不拉伸）
		}

		float offset = cardWidth * overlap;
		float totalSpan = (count - 1) * offset + cardWidth;

		// 计算第一个卡牌的中心 X 坐标
		float startX = (screenSize.X - totalSpan) / 2 + cardWidth / 2;

		// Y 坐标：距离屏幕底部一定距离，并确保卡牌完整显示（假设卡牌中心在底部上方卡牌高度的一半处）

		for (int i = 0; i < count; i++)
			handPositions.Add(new Vector2(startX + i * offset, UnselecteY()));

	}
	#endregion


	public List<CardData> GetSelectedCardList()
	{
		List<CardData> selectedCardList = [];
		foreach (Card card in handCards)
		{
			if (card.IsSelected)
				selectedCardList.Add(card.cardData);
		}
		return selectedCardList;
	}

	private void RebuildHandUI(bool animation = false)
	{
		var sorted = handLogic.cardList;
		UpdateCategorySets();

		GenerateHandPosition(sorted.Count);

		Dictionary<int, Queue<Card>> existing = [];

		foreach (var card in handCards)  // 记录上一次的手牌
		{
			int id = CardData.Serialize(card.cardData);
			if (!existing.ContainsKey(id))
				existing[id] = new Queue<Card>();
			existing[id].Enqueue(card);
		}

		List<Card> newHandCards = [];

		for (int i = 0; i < sorted.Count; i++)
		{
			int id = CardData.Serialize(sorted[i]);
			Vector2 target = handPositions[i];

			Card card;
			if (existing.TryGetValue(id, out Queue<Card> queue))
			{
				card = queue.Dequeue(); // 先进先出
				if (queue.Count == 0)
					existing.Remove(id);
			}
			else
			{
				card = cardScene.Instantiate<Card>();
				AddChild(card);
				card.SetCardData(sorted[i]);
				card.IsBack = false;

				// 新牌起始位置
				Vector2 screenSize = GetViewportRect().Size;
				card.Position = new Vector2(screenSize.X / 2, screenSize.Y / 2);
			}

			newHandCards.Add(card);

			if (animation)
			{
				var tween = CreateTween();
				tween.TweenProperty(card, "position",
					target,
					GameSettings.DEAL_DURATION_TIME / 2)
					.SetTrans(Tween.TransitionType.Quad)
					.SetEase(Tween.EaseType.Out);
			}
			else
			{
				card.Position = target;
			}
			MoveChild(card, i);
		}
		foreach (var queue in existing.Values)
		{
			while (queue.Count > 0)
				queue.Dequeue().QueueFree();
		}
		SetHandCards(newHandCards);
	}

	#region 鼠标输入选牌
	public override void _Input(InputEvent @event)
	{
		//FIXME: 当前的拖拽有问题，没有考虑到图层的遮罩关系，也就是如果从右向左选择卡牌，那么鼠标在很远的位置就会触发选择卡牌
		if (@event is InputEventMouseButton mouse_event)
		{
			if (mouse_event.ButtonIndex == MouseButton.Left)
			{
				if (mouse_event.Pressed)
				{
					isLeftMouseDown = true;
					ProcessMouseSelectCard();
				}
				else
				{
					isLeftMouseDown = false;
				}
			}
		}
		else if (@event is InputEventMouseMotion motionEvent && isLeftMouseDown && lastSelectCard != null)
		{
			ProcessMouseDragCard();
		}
	}

	private void ProcessMouseSelectCard()
	{
		Vector2 mousePos = GetGlobalMousePosition();
		for (int i = handCards.Count - 1; i >= 0; i--)  // 从后往前遍历，确保只处理最上层的卡牌
		{
			Card card = handCards[i];
			if (IsPointOverCard(mousePos, card) && card.CanSelected)
			{
				ToggleCardSelection(card);
				lastSelectCard = card; // 只处理最上层的卡牌
				return;
			}
		}
		lastSelectCard = null;
	}
	private void ProcessMouseDragCard()
	{
		Vector2 mousePos = GetGlobalMousePosition();
		for (int i = handCards.Count - 1; i >= 0; i--)  // 从后往前遍历，确保只处理最上层的卡牌
		{
			Card card = handCards[i];
			if (IsPointOverCard(mousePos, card) && card.CanSelected)
			{
				if (lastSelectCard.IsSelected != card.IsSelected)
					ToggleCardSelection(card);
				break; // 只处理最上层的卡牌
			}
		}
	}

	private bool IsPointOverCard(Vector2 globalPoint, Card card)
	{
		Vector2 cardPos = card.GlobalPosition;
		float halfWidth = CardParams.CARD_WIDTH / 2;
		float halfHeight = CardParams.CARD_HEIGHT / 2;

		return globalPoint.X >= cardPos.X - halfWidth &&
			   globalPoint.X <= cardPos.X + halfWidth &&
			   globalPoint.Y >= cardPos.Y - halfHeight &&
			   globalPoint.Y <= cardPos.Y + halfHeight;
	}

	#endregion
	#region 工具函数
	public void ClearHand()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		ClearHandCard();
		handLogic.RemoveCard([]);
		RebuildHandUI();
	}

	public void RemoveSeletedCard(List<CardData> cardDatas)
	{
		handLogic.RemoveCard(cardDatas);
		RebuildHandUI(true);
	}

	private void SetSelectedPosition(Card card)
	{
		Vector2 postion = card.Position;
		float y = UnselecteY() + (card.IsSelected ? -CardLayoutParams.BOTTOM_MARGIN_MOVE : 0);
		card.Position = new Vector2(postion.X, y);
	}
	public void InsertCard(CardData currentCard)
	{
		handLogic.Insert(currentCard);
		RebuildHandUI(true);
	}
	public void RegenerateCardList(List<CardData> cardDatas, Rank rank, Suit suit)
	{
		handLogic.GenarateCardList(suit, rank, cardDatas);
		RebuildHandUI(true);
	}
	public void InsertCard(List<CardData> cardDatas)
	{
		foreach (CardData card in cardDatas)
			handLogic.Insert(card);
		RebuildHandUI(true);
	}

	public void SetAllCardSelectable(bool selectable, bool isDark = true)
	{
		foreach (var card in handCards)
		{
			SetCardSelectable(card, selectable, isDark);
		}
	}
	public void SetCardUnSelectableExpect(Suit suit)
	{
		// 获取指定花色对应的牌列表（注意：这些列表存储的是 Card 对象，而不是 CardData）
		List<Card> targetCards = suit switch
		{
			Suit.SPADE => spadeCards,
			Suit.CLUB => clubCards,
			Suit.DIAMOND => diamondCards,
			Suit.HEART => heartCards,
			Suit.NONE => mainCards,
			_ => throw new ArgumentOutOfRangeException(nameof(suit))
		};

		bool haveSuit = targetCards.Count != 0;
		if (haveSuit)
		{
			// 遍历所有手牌，根据是否在目标列表中设置可选性
			foreach (var card in handCards)
			{
				// 如果当前牌在目标花色列表中，则设为可选，否则设为不可选
				bool shouldBeSelectable = targetCards.Contains(card);
				SetCardSelectable(card, shouldBeSelectable); // 默认 isDark = true，不可选牌变暗
			}
		}
		else
		{
			// 没有指定花色的牌，所有牌都可选
			SetAllCardSelectable(true);
		}
	}

	public void SetCardSelectable(Card card, bool selectable, bool isDark = true)
	{
		if (selectable == false && isDark)
			card.SetDark();
		if (!isDark)
			card.SetLight();
		card.CanSelected = selectable;
		if (card.IsSelected && selectable == false)  // 设置为不可选当前牌选中的时候要设置为不选中
		{
			ToggleCardSelection(card);
			GD.Print($"当前牌被选中{card.cardData.suit} {card.cardData.rank}设置为不能选，且取消选中");
		}
	}

	public void SetAllCardIsSelected(bool isSelected)
	{
		foreach (var card in handCards)
		{
			card.IsSelected = isSelected;
			if (card.IsSelected != isSelected)
			{
				ToggleCardSelection(card);
			}
		}
	}
	private void ToggleCardSelection(Card card)
	{
		card.IsSelected = !card.IsSelected;
		SetSelectedPosition(card);
		EventBus.OnSelectCardEvent(CardData.Serialize(card.cardData), card.IsSelected);
	}

	public List<Card> GetHandCards()
	{
		return handCards;
	}
	public CardCategory GetCardCategory(Card card)
	{
		if (spadeCards.Contains(card)) return CardCategory.Spade;
		if (heartCards.Contains(card)) return CardCategory.Heart;
		if (clubCards.Contains(card)) return CardCategory.Club;
		if (diamondCards.Contains(card)) return CardCategory.Diamond;
		if (mainCards.Contains(card)) return CardCategory.Main;
		throw new InvalidOperationException("Card not found in any category");
	}
	private void SetHandCards(List<Card> cards)
	{
		handCards = cards;
		spadeCards.Clear();
		heartCards.Clear();
		clubCards.Clear();
		diamondCards.Clear();
		mainCards.Clear();
		foreach (var card in cards)
		{
			int id = CardData.Serialize(card.cardData);
			if (spadeIdSet.Contains(id))
				spadeCards.Add(card);
			else if (heartIdSet.Contains(id))
				heartCards.Add(card);
			else if (clubIdSet.Contains(id))
				clubCards.Add(card);
			else if (diamondIdSet.Contains(id))
				diamondCards.Add(card);
			else if (mainIdSet.Contains(id))
				mainCards.Add(card);
		}
	}
	private void UpdateCategorySets()
	{
		spadeIdSet = [.. CardData.Serialize(handLogic.spadeList)];
		heartIdSet = [.. CardData.Serialize(handLogic.heartList)];
		clubIdSet = [.. CardData.Serialize(handLogic.clubList)];
		diamondIdSet = [.. CardData.Serialize(handLogic.diamondList)];
		mainIdSet = [.. CardData.Serialize(handLogic.mainList)];

		if (!Multiplayer.IsServer()) return;

		// 调试打印
		GD.Print($"【UpdateCategorySets】黑桃 ({spadeIdSet.Count} 张): {string.Join(", ", spadeIdSet)}");
		GD.Print($"红心 ({heartIdSet.Count} 张): {string.Join(", ", heartIdSet)}");
		GD.Print($"梅花 ({clubIdSet.Count} 张): {string.Join(", ", clubIdSet)}");
		GD.Print($"方片 ({diamondIdSet.Count} 张): {string.Join(", ", diamondIdSet)}");
		GD.Print($"主牌 ({mainIdSet.Count} 张): {string.Join(", ", mainIdSet)}");
	}
	private void ClearHandCard()
	{
		handCards.Clear();
		spadeCards.Clear();
		heartCards.Clear();
		clubCards.Clear();
		diamondCards.Clear();
		mainCards.Clear();
	}

	private void RemoveHandCardAt(int index)
	{
		if (index < 0 || index >= handCards.Count) return;
		Card card = handCards[index];
		spadeCards.Remove(card);
		heartCards.Remove(card);
		clubCards.Remove(card);
		diamondCards.Remove(card);
		mainCards.Remove(card);
		handCards.RemoveAt(index);
	}
	#endregion
}
