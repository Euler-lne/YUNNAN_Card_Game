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

		for (int i = 0; i < 4; i++)
		{
			PutAreaLayout putAreaLayout = CalculateLayout(i, screenSize);
			cardAreas[i].Init(putAreaLayout);
			cardAreas[i].Test();
		}

	}
	private PutAreaLayout CalculateLayout(int seatIndex, Vector2 screenSize)
	{
		float putWidth = CardParams.CARD_WIDTH * CardParams.CARD_SCALE;
		float putHeight = CardParams.CARD_HEIGHT * CardParams.CARD_SCALE;

		float margin = CardLayoutParams.PUT_CARD_MARGIN;
		float spacing = CardLayoutParams.PUT_CARD_SPACING;

		float handHeight = CardParams.CARD_HEIGHT;

		// 上下左右的安全偏移
		float horizontalOffset = putHeight + margin;

		// ===== 上下位置 =====
		float bottomY = screenSize.Y - handHeight - spacing - putHeight / 2;
		float topY = margin + putHeight / 2;

		// ===== 左右位置 =====
		float leftX = margin + putHeight / 2;
		float rightX = screenSize.X - margin - putHeight / 2;

		// ===== 左右Y范围（必须避开上下出牌区）=====
		float verticalStart = bottomY - putWidth / 2;
		float verticalEnd = topY + putWidth / 2;

		PutAreaLayout layout = new();

		switch (seatIndex)
		{
			// =====================
			// Bottom
			// =====================
			case 0:
				layout.start = new Vector2(
					horizontalOffset + putWidth / 2,
					bottomY
				);

				layout.end = new Vector2(
					screenSize.X - horizontalOffset - putWidth / 2,
					bottomY
				);

				layout.rotation = 0;
				break;

			// =====================
			// Right
			// =====================
			case 1:
				layout.start = new Vector2(
					rightX,
					verticalStart
				);

				layout.end = new Vector2(
					rightX,
					verticalEnd
				);

				layout.rotation = Mathf.Pi / 2;
				break;

			// =====================
			// Top
			// =====================
			case 2:
				layout.start = new Vector2(
					screenSize.X - horizontalOffset - putWidth / 2,
					topY
				);

				layout.end = new Vector2(
					horizontalOffset + putWidth / 2,
					topY
				);

				layout.rotation = Mathf.Pi;
				break;

			// =====================
			// Left
			// =====================
			case 3:
				layout.start = new Vector2(
					leftX,
					verticalEnd
				);

				layout.end = new Vector2(
					leftX,
					verticalStart
				);

				layout.rotation = Mathf.Pi * 1.5f;
				break;
		}

		return layout;
	}

}
