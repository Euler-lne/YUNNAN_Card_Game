using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;
public partial class PutCardArea : Node2D
{
	[Export] PackedScene cardScene;
	private List<Card> cards = [];
	private PutAreaLayout putAreaLayout;
	private bool isCenter;
	private List<Vector2> putLayout = [];

	public override void _Ready()
	{
		isCenter = true;
	}

	public void Init(PutAreaLayout putAreaLayout)
	{
		this.putAreaLayout = putAreaLayout;
	}

	public PutAreaLayout GetPutAreaLayout() => putAreaLayout;


	public void Insert(List<CardData> cardDatas, bool isBack = false, GamePhase gamePhase = GamePhase.PLAYING)
	{
		isCenter = gamePhase == GamePhase.PLAYING;
		if (!isCenter && cardDatas.Count == 0 && isBack)
		{
			Card card = GenerateCard(isBack, this);
			cards.Add(card);
		}
		else
			foreach (var cardData in cardDatas)
			{
				Card card = GenerateCard(isBack, this, cardData);
				cards.Add(card);
			}
		GenerateLayout();
	}

	public List<CardData> RemoveCards()
	{
		List<CardData> cardDatas = [];
		foreach (var card in cards)
		{
			card.QueueFree();
			cardDatas.Add(card.cardData);
		}
		cards.Clear();
		putLayout.Clear();
		return cardDatas;
	}

	public List<Card> GetCards() => cards;

	public List<Card> RemoveCardsExpectPoint()
	{
		List<Card> pointCards = [];
		foreach (var card in cards)
		{
			if (card.cardData.IsPoint())
				pointCards.Add(card);
			else
				card.QueueFree();
		}
		cards.Clear();
		putLayout.Clear();
		return pointCards;
	}

	public void Test()
	{
		for (int i = 0; i < 9; i++)
		{
			Card card = GenerateCard(true, this);
			cards.Add(card);
		}
		GenerateLayout();
	}
	private Card GenerateCard(bool isBack, Node2D parent, CardData cardData = null)
	{
		Card card = cardScene.Instantiate<Card>();
		parent.AddChild(card);
		if (cardData != null)
			card.SetCardData(cardData);
		card.IsBack = isBack;
		card.SetLight();
		card.Scale = new(CardParams.CARD_PUT_SCALE * CardParams.CARD_SCALE, CardParams.CARD_PUT_SCALE * CardParams.CARD_SCALE);
		return card;
	}

	private void GenerateLayout()
	{
		putLayout.Clear();

		int total = cards.Count;
		if (total == 0) return;

		Vector2 start = putAreaLayout.start;
		Vector2 end = putAreaLayout.end;

		Vector2 dir = end - start;
		float length = dir.Length();
		dir = dir.Normalized();

		float cardSize = CardParams.CARD_WIDTH * CardParams.CARD_PUT_SCALE;

		float spacing;
		if ((total - 1) * cardSize <= length)
		{
			// 不重叠
			spacing = cardSize;
		}
		else
		{
			// 压缩重叠
			spacing = length / (total - 1);

		}

		if (isCenter)
		{
			float totalWidth = (total - 1) * spacing;
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

	public List<Card> GenerateCard(int[] cardIds, TableManager parent)
	{
		List<Card> cards = [];
		foreach (int cardId in cardIds)
		{
			Card card = GenerateCard(true, parent, CardData.Deserialize(cardId));
			cards.Add(card);
		}
		return cards;
	}
}
