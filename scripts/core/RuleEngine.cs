using System.Collections.Generic;

public static class RuleEngine
{

    #region 发牌定主
    public static DeclareOption GetDeclareOption(
        List<CardData> hand,
        TrumpState trumpState,
        Rank currentLevel)
    {
        if (trumpState.isLocked)  // 已经定主了
            return DeclareOption.NONE;

        // 还没有主
        if (trumpState.trumpSuit == Suit.NONE && !trumpState.haveTrump)
        {
            if (hand.Count == 1 && hand[0].rank == currentLevel) // 只有第一轮可以暗主
                return DeclareOption.DARK_TRUMP;
            if (HasRankCard(hand, currentLevel))
                return DeclareOption.BRIGHT_TRUMP;
        }
        else if (CanCounterTrump(hand, trumpState, currentLevel) && trumpState.haveTrump)
            return DeclareOption.COUNTER_TRUMP;  // 有主的情况下才能反


        return DeclareOption.NONE;
    }

    public static bool CanDeclareOfOption(List<CardData> hand, TrumpState trumpState, Rank currentLevel, DeclareOption option)
    {
        bool answer = false;
        switch (option)
        {
            case DeclareOption.BRIGHT_TRUMP:
                answer = HasRankCard(hand, currentLevel);
                break;
            case DeclareOption.COUNTER_TRUMP:
                answer = CanCounterTrump(hand, trumpState, currentLevel);
                break;
            case DeclareOption.DARK_TRUMP:
                answer = hand.Count == 1 && hand[0].rank == currentLevel;
                break;
        }
        return answer;
    }

    private static bool HasRankCard(List<CardData> hand, Rank currentLevel)
    {
        // 只要有就可以亮主
        foreach (var card in hand)
        {
            if (card.rank == currentLevel)
                return true;
        }

        return false;
    }

    private static bool CanCounterTrump(List<CardData> hand, TrumpState trumpState, Rank currentLevel)
    {
        var suitCount = new Dictionary<Suit, int>();

        foreach (var card in hand)
        {
            if (card.rank != currentLevel || card.suit == trumpState.trumpSuit)
                continue;
            // 当前是满足的牌的型号相等了
            if (!suitCount.ContainsKey(card.suit))
                suitCount[card.suit] = 0;

            suitCount[card.suit]++;

            if (suitCount[card.suit] >= 2)
                return true;
        }

        return false;

    }
    #endregion

    #region 选牌逻辑
    public static bool CanSelect(CardData card, GamePhase phase, Rank rank)
    {
        if (phase == GamePhase.DECLARE)
            return card.rank == rank;

        return true;
    }

    public static bool IsDeclareRight(DeclareOption option, List<CardData> cardDatas, Rank rank)
    {
        switch (option)
        {
            case DeclareOption.NONE:
                break;
            case DeclareOption.BRIGHT_TRUMP:
                return cardDatas.Count == 1 && cardDatas[0].rank == rank;
            case DeclareOption.COUNTER_TRUMP:
                return cardDatas.Count == 2 && cardDatas[0].rank == rank && cardDatas[1].rank == rank && cardDatas[0].suit == cardDatas[1].suit;
            case DeclareOption.DARK_TRUMP:
                return true;
        }
        return false;
    }

    public static int GetCardValue(CardData cardData, Suit trumpSuit, Rank trumpRank)
    {
        if (cardData.suit == trumpSuit && cardData.rank == Rank.FIVE)
            return 20;
        else if (cardData.suit == Suit.NONE && cardData.rank == Rank.BIG_JOKER)
            return 19;
        else if (cardData.suit == Suit.NONE && cardData.rank == Rank.SMALL_JOKER)
            return 18;
        else if (cardData.suit == Suit.SPADE && cardData.rank == Rank.ACE)
            return 17;
        else if (cardData.suit == trumpSuit && cardData.rank == trumpRank)
            return 16;
        else if (cardData.rank == trumpRank) //FIXME:副主算主吗？
            return 15;
        return (int)cardData.rank;
    }
    #endregion

}
