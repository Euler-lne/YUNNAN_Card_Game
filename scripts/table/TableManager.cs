using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
public partial class TableManager : Node2D
{
	private readonly Dictionary<int, PutCardArea> cardAreas = [];

	public override void _Ready()
	{
		Vector2 screenSize = GetViewportRect().Size;
		cardAreas[0] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/ButtomArea");
		cardAreas[1] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/RightArea");
		cardAreas[2] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/TopArea");
		cardAreas[3] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/LeftArea");

		for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
		{
			PutAreaLayout putAreaLayout = CalculateLayout(i, screenSize);
			cardAreas[i].Init(putAreaLayout);
			// cardAreas[i].Test();
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
	private PutAreaLayout CalculateLayout(int seatIndex, Vector2 screenSize)
	{
		float putWidth = CardParams.CARD_WIDTH * CardParams.CARD_PUT_SCALE;
		float putHeight = CardParams.CARD_HEIGHT * CardParams.CARD_PUT_SCALE;

		float margin = CardLayoutParams.PUT_CARD_MARGIN;
		float spacing = CardLayoutParams.PUT_CARD_SPACING;

		float handHeight = CardParams.CARD_HEIGHT;

		// 上下左右的安全偏移
		float horizontalOffset = putHeight / 2 + putWidth / 2 + margin;

		// ===== 上下位置 =====
		float bottomY = screenSize.Y - handHeight - spacing - putHeight / 2;
		float topY = margin + putHeight / 2;

		// ===== 左右位置 =====
		float leftX = margin + putHeight / 2;
		float rightX = screenSize.X - margin - putHeight / 2;

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
	#endregion
}
