using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
using Euler.Event;
using System.Linq;
public partial class TableManager : Node2D
{
	private readonly Dictionary<int, PutCardArea> cardAreas = [];
	private Vector2 center = new();
	private Vector2 scorePos = new(CardLayoutParams.SCORE_POINT_X, CardLayoutParams.SCORE_POINT_Y);

	private List<Card> pointCards = [];

	public override void _Ready()
	{
		cardAreas[0] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/ButtomArea");
		cardAreas[1] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/RightArea");
		cardAreas[2] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/TopArea");
		cardAreas[3] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/LeftArea");

		for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
		{
			PutAreaLayout putAreaLayout = CalculateLayout(i);
			cardAreas[i].Init(putAreaLayout);
			// cardAreas[i].Test();
		}
		center = GetCenter();

		TurnEvent.NewTurnEvent += OnNewTurnEvent;
		TurnEvent.ExpandScoreCardEvent += OnExpandScoreCardEvent;
		TurnEvent.MoveCardToScoreEvent += OnMoveCardToScoreEvent;
		TurnEvent.ExpandTableCardEvent += OnExpandTableCardEvent;
		TurnEvent.CancelThrowCardEvent += OnCancelThrowCardEvent;
		TurnEvent.ClearPointCardsEvent += OnClearPointCardsEvent;
	}
	public override void _EnterTree()
	{
		TurnEvent.NewTurnEvent -= OnNewTurnEvent;
		TurnEvent.ExpandScoreCardEvent -= OnExpandScoreCardEvent;
		TurnEvent.MoveCardToScoreEvent -= OnMoveCardToScoreEvent;
		TurnEvent.ExpandTableCardEvent -= OnExpandTableCardEvent;
		TurnEvent.CancelThrowCardEvent -= OnCancelThrowCardEvent;
		TurnEvent.ClearPointCardsEvent -= OnClearPointCardsEvent;
	}

	private void OnClearPointCardsEvent()
	{
		foreach (var card in pointCards)
		{
			card.QueueFree();
		}
		pointCards.Clear();
	}

	private void OnCancelThrowCardEvent()
	{
		foreach (var card in pointCards)
		{
			card.QueueFree();
		}
		pointCards.Clear();
	}

	private void OnExpandTableCardEvent(int[] cardIds)
	{
		pointCards = cardAreas[0].GenerateCard(cardIds, this);
		OnExpandScoreCardEvent(pointCards.Count);
	}

	private void OnMoveCardToScoreEvent(int cardId)
	{
		int index = 0;
		for (; index < pointCards.Count; index++)
		{
			if (CardData.Serialize(pointCards[index].cardData) == cardId)
				break;
		}
		if (index == pointCards.Count)
		{
			GD.PrintErr($"出现问题，客户端{Multiplayer.GetUniqueId()}里面没有服务器的卡牌{cardId}");
			return;
		}
		Card card = pointCards[index];
		pointCards.RemoveAt(index);
		var tween = CreateTween();
		card.Rotation = 0f;
		tween.TweenProperty(
			card,
			"global_position",
			scorePos,
			GameSettings.MOVE_DURATION_TIME / 2)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(card.QueueFree));

	}

	private void OnExpandScoreCardEvent(int len)
	{
		if (len != pointCards.Count)
		{
			GD.PrintErr($"出现问题，当前服务器的分卡数量{len}，客户端{Multiplayer.GetUniqueId()}的分卡数量{pointCards.Count}");
		}
		List<Vector2> expandArea = GenerateExpandArea(len);
		for (int i = 0; i < pointCards.Count; i++)
		{
			Card card = pointCards[i];
			// 创建移动到屏幕中心的动画
			var tween = CreateTween();
			card.Rotation = 0f;
			tween.TweenProperty(
				card,
				"global_position",
				expandArea[i],
				GameSettings.EXPAND_DURATION_TIME)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}
	}

	private void OnNewTurnEvent(bool isDealer, int dealerSeat)
	{
		if (isDealer)
		{
			for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
			{
				cardAreas[i].RemoveCards();
			}
		}
		else
		{
			List<Card> newCards = [];
			for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
			{
				int index = (i + dealerSeat) % GameSettings.PLAYER_COUNT;
				List<Card> cards = cardAreas[index].RemoveCardsExpectPoint();
				newCards.AddRange(cards);
			}
			MovePointCardToCenter(newCards);
			pointCards.AddRange(newCards);
		}

	}

	private void MovePointCardToCenter(List<Card> newCards)
	{
		foreach (var card in newCards)
		{
			// 确保父节点为当前节点
			if (card.GetParent() != this)
			{
				Vector2 globalPos = card.GlobalPosition;
				card.GetParent()?.RemoveChild(card);
				AddChild(card);
				card.GlobalPosition = globalPos;
			}

			// 创建移动到屏幕中心的动画
			var tween = CreateTween();
			card.Rotation = 0f;
			tween.TweenProperty(
				card,
				"global_position",
				center,
				GameSettings.MOVE_DURATION_TIME / 2)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}
	}

	#region 操作出牌
	public void InsertCard(int seat, List<CardData> cardDatas, bool isBack = false, GamePhase gamePhase = GamePhase.PLAYING)
	{
		cardAreas[seat].Insert(cardDatas, isBack, gamePhase);
	}
	public List<CardData> RemoveAt(int seat)
	{
		return cardAreas[seat].RemoveCards();
	}
	#endregion
	#region 工具函数
	private PutAreaLayout CalculateLayout(int seatIndex)
	{
		Vector2 screenSize = GetViewportRect().Size;
		float putWidth = CardParams.CARD_WIDTH * CardParams.CARD_PUT_SCALE;
		float putHeight = CardParams.CARD_HEIGHT * CardParams.CARD_PUT_SCALE;

		float margin = CardLayoutParams.PUT_CARD_MARGIN;
		float topMargin = CardLayoutParams.PUT_CARD_TOP_MARGIN;
		float hMargin = CardLayoutParams.PUT_CARD_H_MARGIN;
		float spacing = CardLayoutParams.PUT_CARD_SPACING;

		float handHeight = CardParams.CARD_HEIGHT;

		// 上下左右的安全偏移
		float horizontalOffset = putHeight / 2 + putWidth / 2 + margin;

		// ===== 上下位置 =====
		float bottomY = screenSize.Y - handHeight - spacing - putHeight / 2;
		float topY = topMargin + putHeight / 2;

		// ===== 左右位置 =====
		float leftX = hMargin + putHeight / 2;
		float rightX = screenSize.X - hMargin - putHeight / 2;

		PutAreaLayout layout = new();

		switch (seatIndex)
		{
			case 0:
				layout.start = new Vector2(
					leftX + horizontalOffset,
					bottomY
				);

				layout.end = new Vector2(
					rightX - horizontalOffset,
					bottomY
				);

				layout.rotation = 0;
				layout.isHorizontal = false;
				break;

			case 1:
				layout.start = new Vector2(
					rightX,
					bottomY
				);

				layout.end = new Vector2(
					rightX,
					topY
				);

				layout.rotation = Mathf.Pi / 2;
				layout.isHorizontal = true;

				break;

			case 2:
				layout.start = new Vector2(
					rightX - horizontalOffset,
					topY
				);

				layout.end = new Vector2(
					leftX + horizontalOffset,
					topY
				);

				layout.rotation = Mathf.Pi;
				layout.isHorizontal = false;

				break;

			case 3:
				layout.start = new Vector2(
					leftX,
					topY
				);

				layout.end = new Vector2(
					leftX,
					bottomY
				);

				layout.rotation = Mathf.Pi * 1.5f;
				layout.isHorizontal = true;

				break;
		}

		return layout;
	}
	private Vector2 GetCenter()
	{
		PutAreaLayout buttom = cardAreas[0].GetPutAreaLayout();
		PutAreaLayout right = cardAreas[1].GetPutAreaLayout();
		float x = (buttom.start.X + buttom.end.X) / 2;
		float y = (right.start.Y + right.end.Y) / 2;
		return new(x, y);
	}

	private List<Vector2> GenerateExpandArea(int count)
	{
		List<Vector2> layout = [];
		PutAreaLayout buttom = cardAreas[0].GetPutAreaLayout();
		float putHeight = CardParams.CARD_HEIGHT * CardParams.CARD_PUT_SCALE;
		float startX = buttom.start.X - putHeight / 2, endX = buttom.end.X + putHeight / 2;
		float margin = CardLayoutParams.PUT_CARD_MARGIN;
		float Y = center.Y + putHeight + margin;
		// 单张牌直接居中
		if (count == 1)
		{
			float centerX = (startX + endX) / 2;
			layout.Add(new Vector2(centerX, Y));
			return layout;
		}
		float cardWidth = CardParams.CARD_WIDTH * CardParams.CARD_PUT_SCALE;
		float totalWidth = endX - startX;

		// 计算牌与牌之间的间隔
		float spacing;
		if (count * cardWidth <= totalWidth)
		{
			// 不重叠，保持 cardWidth 间距
			spacing = cardWidth;
		}
		else
		{
			// 需要重叠压缩，均匀分布
			spacing = totalWidth / (count - 1);
		}

		// 居中计算起始 X 坐标
		float totalSpan = (count - 1) * spacing;
		float startCenterX = (startX + endX - totalSpan) / 2;
		for (int i = 0; i < count; i++)
		{
			float x = startCenterX + i * spacing;
			layout.Add(new Vector2(x, Y));
		}
		return layout;
	}
	#endregion
}
