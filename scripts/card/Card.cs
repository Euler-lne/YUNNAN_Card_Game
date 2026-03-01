using Godot;
using System;

public partial class Card : Node2D
{
	public CardData cardData;
	private Sprite2D sprite;
	public bool isSelected;
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		isSelected = false;
	}

	public override void _Process(double delta)
	{
	}

	public void SetCardData(CardData data)
	{
		// GD.Print(Scale);
		cardData = data;
		UpdateTexture();
	}

	private void UpdateTexture()
	{
		string path = GetCardTexturePath();
		sprite.Texture = GD.Load<Texture2D>(path);
	}

	private string GetCardTexturePath()
	{
		string rank = "", color = "", suit = "", human = "", prefix = "res://assets/";
		switch (cardData.suit)
		{
			case Suit.CLUB:
				suit = "club";
				color = "black";
				break;
			case Suit.DIAMOND:
				suit = "diamond";
				color = "red";
				break;
			case Suit.HEART:
				suit = "heart";
				color = "red";
				break;
			case Suit.SPADE:
				suit = "spade";
				color = "black";
				break;
			case Suit.NONE:
				if (cardData.rank == Rank.BIG_JOKER)
					return prefix + "joker_big.png";
				if (cardData.rank == Rank.SMALL_JOKER)
					return prefix + "joker_little.png";
				break;
		}
		switch (cardData.rank)
		{
			case Rank.ACE:
				rank = "ace";
				break;
			case Rank.TWO:
				rank = "2";
				break;
			case Rank.THREE:
				rank = "3";
				break;
			case Rank.FOUR:
				rank = "4";
				break;
			case Rank.FIVE:
				rank = "5";
				break;
			case Rank.SIX:
				rank = "6";
				break;
			case Rank.SEVEN:
				rank = "7";
				break;
			case Rank.EIGHT:
				rank = "8";
				break;
			case Rank.NINE:
				rank = "9";
				break;
			case Rank.TEN:
				rank = "10";
				break;
			case Rank.JACK:
				rank = "jack";
				human = "human_";
				break;
			case Rank.QUEEN:
				rank = "queen";
				human = "human_";
				break;
			case Rank.KING:
				rank = "king";
				human = "human_";
				break;
		}
		return prefix + human + rank + "_of_" + suit + "_" + color + "_design_2.png";
	}
}
