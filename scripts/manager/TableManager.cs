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
		float halfWidth = CardParams.CARD_WIDTH / 2;
		float halfHeight = CardParams.CARD_HEIGHT / 2;
		float y = screenSize.Y - halfHeight * 2;
		cardAreas[0] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/ButtomArea");
		cardAreas[0].originPosition = new(halfWidth + CardLayoutParams.CARD_PUT_DECLARE_LEFT_OFFSET, y - halfHeight - CardLayoutParams.CARD_PUT_DOWN_OFFSET);
		cardAreas[0].rotation = 0;
		cardAreas[0].Test();

		cardAreas[1] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/RightArea");
		cardAreas[1].originPosition = new(screenSize.X - halfHeight - CardLayoutParams.CARD_PUT_DOWN_OFFSET, y - halfWidth - CardLayoutParams.CARD_PUT_DECLARE_LEFT_OFFSET - halfWidth);
		cardAreas[1].rotation = Mathf.Pi / 2;
		cardAreas[1].Test();

		cardAreas[2] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/TopArea");
		cardAreas[2].originPosition = new(screenSize.X - halfWidth - CardLayoutParams.CARD_PUT_DECLARE_LEFT_OFFSET, halfHeight + CardLayoutParams.CARD_PUT_DOWN_OFFSET);
		cardAreas[2].rotation = Mathf.Pi;
		cardAreas[2].Test();

		cardAreas[3] = GetNode<PutCardArea>("../PlayeArea/PutCardArea/LeftArea");
		cardAreas[3].originPosition = new(CardLayoutParams.CARD_PUT_DOWN_OFFSET + halfHeight, CardLayoutParams.CARD_PUT_DECLARE_LEFT_OFFSET + halfWidth);
		cardAreas[3].rotation = Mathf.Pi / 2 * 3;
		cardAreas[3].Test();

	}
}
