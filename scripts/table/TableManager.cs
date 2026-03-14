using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
using Euler.Event;
public partial class TableManager : Node2D
{
	private readonly Dictionary<int, PutCardArea> cardAreas = [];
	private Vector2 center = new();

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

	}
	public override void _EnterTree()
	{
		TurnEvent.NewTurnEvent -= OnNewTurnEvent;

	}

	private void OnNewTurnEvent(bool isDealer)
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
				List<Card> cards = cardAreas[i].RemoveCardsExpectPoint();
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
				GameSettings.DEAL_DURATION_TIME / 2)
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
		PutAreaLayout buttom = CalculateLayout(0);
		PutAreaLayout right = CalculateLayout(1);
		float x = (buttom.start.X + buttom.end.X) / 2;
		float y = (right.start.Y + right.end.Y) / 2;
		return new(x, y);
	}
	#endregion
}
