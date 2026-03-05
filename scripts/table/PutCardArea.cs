using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
public partial class PutCardArea : Node2D
{
	[Export] PackedScene cardScene; // 测试用
	private List<Card> cards = [];
	private PutAreaLayout putAreaLayout;
	private bool isCenter;
	private List<Vector2> putLayout = [];

	public override void _Ready()
	{
		isCenter = false;
	}

	public void Init(PutAreaLayout putAreaLayout)
	{
		this.putAreaLayout = putAreaLayout;
	}

	public void Test()
	{
		for (int i = 0; i < 20; i++)
		{
			Card card = cardScene.Instantiate<Card>();
			AddChild(card);
			cards.Add(card);
			card.Scale = new(CardParams.CARD_PUT_SCALE * CardParams.CARD_SCALE, CardParams.CARD_PUT_SCALE * CardParams.CARD_SCALE);
		}
		GenerateLayout();
	}

	public void GenerateLayout()
	{
		putLayout.Clear();

		int total = cards.Count;
		if (total == 0) return;

		Vector2 start = putAreaLayout.start;
		Vector2 end = putAreaLayout.end;

		Vector2 dir = (end - start);
		float length = dir.Length();
		dir = dir.Normalized();

		float cardSize =
			Mathf.Abs(putAreaLayout.rotation % Mathf.Pi) < 0.01f
			? CardParams.CARD_WIDTH * CardParams.CARD_PUT_SCALE
			: CardParams.CARD_HEIGHT * CardParams.CARD_PUT_SCALE;

		float spacing;

		// ==========================
		// ✅ 核心修改点
		// ==========================
		if (total * cardSize <= length)
		{
			// 不重叠
			spacing = cardSize;
		}
		else
		{
			// 压缩重叠
			spacing = (length - cardSize) / (total - 1);
		}

		if (isCenter)
		{
			float totalWidth = cardSize + (total - 1) * spacing;
			Vector2 center = start + dir * (length / 2f);
			Vector2 firstPos = center - dir * (totalWidth / 2f);

			for (int i = 0; i < total; i++)
			{
				putLayout.Add(firstPos + dir * (i * spacing));
			}
		}
		else
		{
			for (int i = 0; i < total; i++)
			{
				putLayout.Add(start + dir * (i * spacing));
			}
		}

		// 应用
		for (int i = 0; i < total; i++)
		{
			cards[i].Position = putLayout[i];
			cards[i].Rotation = putAreaLayout.rotation;
		}
	}

}
