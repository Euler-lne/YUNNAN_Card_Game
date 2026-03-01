public class MainCardEvaluator
{
    public Suit mainSuit;
    public Rank levelRank;

    public bool IsMain(CardData card)
    {
        if (card.rank == Rank.BIG_JOKER || card.rank == Rank.SMALL_JOKER)
            return true;

        if (card.rank == levelRank)
            return true;

        if (card.suit == Suit.SPADE && card.rank == Rank.ACE)
            return true;

        if (card.suit == mainSuit)
            return true;

        return false;
    }
}