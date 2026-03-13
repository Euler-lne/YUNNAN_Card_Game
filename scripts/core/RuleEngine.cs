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
        if (trumpRank != Rank.FIVE)
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
            else if (cardData.rank == trumpRank)
                return 15;
            // 主花色非中心5、非主等级的牌
            return GetSuitRankIndex(cardData.rank, cardData.suit, trumpSuit, trumpRank, 14);
        }
        else // trumpRank == Rank.FIVE
        {
            if (cardData.suit == trumpSuit && cardData.rank == Rank.FIVE)
                return 20;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.BIG_JOKER)
                return 19;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.SMALL_JOKER)
                return 18;
            else if (cardData.suit == Suit.SPADE && cardData.rank == Rank.ACE)
                return 17;
            else if (cardData.rank == trumpRank)  // 副主
                return 16;
            return GetSuitRankIndex(cardData.rank, cardData.suit, trumpSuit, trumpRank, 15);
        }
    }

    // 辅助方法：计算主花色普通牌在同花色剩余点数中的排名（返回连续值，最大14，最小可能为1）
    private static int GetSuitRankIndex(Rank rank, Suit suit, Suit trumpSuit, Rank trumpRank, int start)
    {
        // 收集该花色剩余的点数（从大到小排序）
        List<Rank> remaining = [];
        for (Rank r = Rank.ACE; r >= Rank.TWO; r--)
        {
            // 排除被提升的牌：中心5、主花色主等级、黑桃A
            if (suit == trumpSuit && r == Rank.FIVE) continue;  // 中心5
            if (r == trumpRank) continue;  // 正主和副主
            if (suit == Suit.SPADE && r == Rank.ACE) continue;  // 常主
            remaining.Add(r);
        }
        int index = remaining.IndexOf(rank); // 找到当前点数的索引（0-based）
        if (index < 0) return -1; // 理论上不会发生
        return start - index;
    }
    #endregion

    #region 出牌判断
    public static PlayType DetermineSelectedPlayType(List<int> selectedCards, CardData trumpCardData)
    {
        if (selectedCards.Count == 0) return PlayType.NONE;
        if (selectedCards.Count == 1) return PlayType.SINGLE;
        if (selectedCards.Count == 2)
        {
            Rank first = CardData.Deserialize(selectedCards[0]).rank;
            Rank second = CardData.Deserialize(selectedCards[1]).rank;
            if (first == second) return PlayType.DOUBLE;
        }
        if (selectedCards.Count % 2 == 0)
        {
            // 判断是否为姊妹对

            return PlayType.EVEN_CORRECT;
        }
        return PlayType.THROW_CARD;
    }
    #endregion


}
