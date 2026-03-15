using System;
using System.Collections.Generic;
using Godot;

public class DeckManager
{
    // 负责发牌，洗牌
    private List<CardData> deck;
    private Random random;

    public DeckManager()
    {
        deck = new List<CardData>();
        random = new Random();
    }

    public void CreateDeck()
    {
        deck.Clear();

        // 两副牌
        for (int i = 0; i < 2; i++)
        {
            foreach (Suit suit in Enum.GetValues<Suit>())
            {
                if (suit == Suit.NONE)
                    continue;
                foreach (Rank rank in Enum.GetValues<Rank>())
                {
                    if (rank == Rank.SMALL_JOKER || rank == Rank.BIG_JOKER)
                        continue;

                    deck.Add(new CardData(suit, rank));
                }
            }

            deck.Add(new CardData(Suit.NONE, Rank.SMALL_JOKER));
            deck.Add(new CardData(Suit.NONE, Rank.BIG_JOKER));
        }
    }

    public void TestCreateDeck()
    {
        deck.Clear();
        deck.Add(new CardData(Suit.NONE, Rank.SMALL_JOKER));
        deck.Add(new CardData(Suit.NONE, Rank.BIG_JOKER));
        deck.Add(new CardData(Suit.NONE, Rank.SMALL_JOKER));
        deck.Add(new CardData(Suit.NONE, Rank.BIG_JOKER));
        for (int j = 0; j < 2; j++)
        {
            Rank[] ranks = Enum.GetValues<Rank>();
            for (int i = ranks.Length - 1; i >= 0; i--)
            {
                Rank rank = ranks[i];
                if (rank == Rank.SMALL_JOKER || rank == Rank.BIG_JOKER) continue;
                foreach (Suit suit in Enum.GetValues<Suit>())
                {
                    if (suit == Suit.NONE) continue;
                    deck.Add(new CardData(suit, rank));
                }
            }
        }
    }

    public void Shuffle()
    {
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            CardData value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
    }

    public CardData DrawCard()
    {
        if (deck.Count == 0)
            return null;

        CardData card = deck[^1];
        deck.RemoveAt(deck.Count - 1);
        return card;
    }

    public List<CardData> GetRestCard()
    {
        return deck;
    }



    public int GetRemainingCount()
    {
        return deck.Count;
    }
}