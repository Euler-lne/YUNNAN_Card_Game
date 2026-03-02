using System;
using System.Collections.Generic;

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
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                if (suit == Suit.NONE)
                    continue;
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
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

        CardData card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    public int GetRemainingCount()
    {
        return deck.Count;
    }
}