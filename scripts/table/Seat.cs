using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;

public partial class Seat : Node2D
{
	[Export] public PackedScene cardScene;
	public Node2D handRoot;
	public Node2D playedRoot;

	public bool isLocalPlayer = false;
	private List<Card> handCards = [];
	private List<Vector2> handPositions = [];
	private Card lastSelectCard = null;

	private bool isLeftMouseDown = false; // 左键是否按住

	private CardList handLogic = new();


	public override void _Ready()
	{
		handRoot = GetNode<Node2D>("HandRoot");
		playedRoot = GetNode<Node2D>("PlayedRoot");
	}
	#region 计算手牌应该出现的位置
	public void GenerateHandPosition(int count)
	{
		handPositions.Clear();
		if (count == 0) return;

		// 获取屏幕尺寸
		Vector2 screenSize = GetViewportRect().Size;

		float cardWidth = CardParams.CARD_WIDTH;
		float cardHeight = CardParams.CARD_HEIGHT; // 确保定义了卡牌高度


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
		float y = screenSize.Y + cardHeight / 2; // 手牌顶部位于屏幕底部
		y -= CardLayoutParams.BOTTOM_MARGIN_UNSELECT; // 当前手牌露出卡牌的0.75倍高度

		for (int i = 0; i < count; i++)
			handPositions.Add(new Vector2(startX + i * offset, y));

	}
	#endregion

	public void InsertCard(CardData currentCard)
	{
		handLogic.Insert(currentCard);
		RebuildHandUI(true);
	}

	public void SetAllCardSelectable(bool selectable)
	{
		foreach (var card in handCards)
		{
			card.CanSelected = selectable;
		}
	}

	public List<Card> GetHandCards()
	{
		return handCards;
	}

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
				handRoot.AddChild(card);
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
			handRoot.MoveChild(card, i);
		}

		handCards = newHandCards;
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

	private void ToggleCardSelection(Card card)
	{
		card.IsSelected = !card.IsSelected;
		Vector2 postion = card.Position;
		float offset = card.IsSelected ? -CardLayoutParams.BOTTOM_MARGIN_MOVE : CardLayoutParams.BOTTOM_MARGIN_MOVE;
		card.Position = new Vector2(postion.X, postion.Y + offset);
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
	private void ClearHand()
	{
		foreach (Node child in handRoot.GetChildren())
			child.QueueFree();

		handCards.Clear();
	}

}
