using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
public partial class PutCardArea : Node2D
{
	[Export] PackedScene cardScene; // 测试用
	private List<Card> cards = [];
	public Vector2 originPosition;
	public float rotation;

	public override void _Ready()
	{

	}

	public void Test()
	{
		Card card = cardScene.Instantiate<Card>();
		AddChild(card);
		card.Scale = new(CardParams.CARD_PUT_SCALE, CardParams.CARD_PUT_SCALE);
		card.Position = originPosition;
		card.Rotation = rotation;
	}


}
