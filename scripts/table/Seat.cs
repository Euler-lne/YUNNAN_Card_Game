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

	public override void _Ready()
	{
		handRoot = GetNode<Node2D>("HandRoot");
		playedRoot = GetNode<Node2D>("PlayedRoot");
	}

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

	/// <summary>
	/// 展示当前手牌
	/// </summary>
	/// <param name="hand"></param>
	public void ShowHand(List<CardData> hand)
	{
		GenerateHandPosition(hand.Count);
		ClearHand();

		for (int i = 0; i < hand.Count; i++)
		{
			Card card = cardScene.Instantiate<Card>();
			handRoot.AddChild(card); // cardParent 可以是任意 Node2D，但建议是一个全屏的 Control 或 Node2D
			card.Position = new Vector2(handPositions[i].X, handPositions[i].Y);
			card.SetCardData(hand[i]);

			handCards.Add(card);
		}
	}
	/// <summary>
	/// 发牌时候调用的函数
	/// </summary>
	/// <param name="hand">发的牌已经被记录了</param>
	/// <param name="dealedCard">发的牌</param>
	public void DealCard(List<CardData> hand, CardData dealedCard)
	{
		GenerateHandPosition(hand.Count);
		ClearHand();

		// 序列化处理，确保可以正确识别这张牌
		int dealedId = CardData.Serialize(dealedCard);
		bool dealt = false; // 标记是否已经处理了当前发牌

		for (int i = 0; i < hand.Count; i++)
		{
			Card card = cardScene.Instantiate<Card>();
			handRoot.AddChild(card);

			// 先生成在牌堆位置
			Vector2 screenSize = GetViewportRect().Size;
			card.Position = new Vector2(screenSize.X / 2, screenSize.Y / 2);
			card.SetCardData(hand[i]);
			handCards.Add(card);

			Vector2 targetPos = new(handPositions[i].X, handPositions[i].Y);

			// 判断是否是当前发的牌，并且只处理一次
			if (!dealt && CardData.Serialize(hand[i]) == dealedId)
			{
				dealt = true;

				var tween = CreateTween();
				tween.TweenProperty(card, "position", targetPos, GameSettings.DEAL_DURATION_TIME / 2)
					 .SetTrans(Tween.TransitionType.Quad)
					 .SetEase(Tween.EaseType.Out);
			}
			else
			{
				// 其他牌直接放在目标位置
				card.Position = targetPos;
			}
		}
	}

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
			// 左键按住并移动：重新检测卡牌
			// GD.Print("Mouse moved");
			ProcessMouseDragCard();
		}
	}

	private void ProcessMouseSelectCard()
	{
		Vector2 mousePos = GetGlobalMousePosition();
		for (int i = handCards.Count - 1; i >= 0; i--)  // 从后往前遍历，确保只处理最上层的卡牌
		{
			Card card = handCards[i];
			if (IsPointOverCard(mousePos, card))
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
			if (IsPointOverCard(mousePos, card))
			{
				if (lastSelectCard.isSelected != card.isSelected)
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

	private void ToggleCardSelection(Card card)
	{
		card.isSelected = !card.isSelected;
		Vector2 postion = card.Position;
		float offset = card.isSelected ? -CardLayoutParams.BOTTOM_MARGIN_MOVE : CardLayoutParams.BOTTOM_MARGIN_MOVE;
		card.Position = new Vector2(postion.X, postion.Y + offset);
	}

	private void ClearHand()
	{
		foreach (Node child in handRoot.GetChildren())
			child.QueueFree();

		handCards.Clear();
	}

}
