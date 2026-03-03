using System;
using System.Collections.Generic;

public static class RuleEngine
{

    #region 发牌定主
    public static DeclareOption GetDeclareOption(
        List<CardData> hand,
        TrumpState trumpState,
        Rank currentLevel)
    {

        // 还没有主
        if (trumpState.trumpSuit == TrumpSuit.UNKNOW_TRUMP && !trumpState.isNoTrump)
        {
            if (HasRankCard(hand, currentLevel))
                return DeclareOption.BRIGHTTRUMP;

            if (hand.Count == 1 && hand[0].rank == currentLevel) // 只有第一轮可以暗主
                return DeclareOption.DARKTRUMP;
        }
        else
        {
            if (CanCounterTrump(hand, trumpState, currentLevel))
                return DeclareOption.COUNTERTRUMP;
        }

        return DeclareOption.NONE;
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
            if (card.rank != currentLevel)
                continue;

            if (!suitCount.ContainsKey(card.suit))
                suitCount[card.suit] = 0;

            suitCount[card.suit]++;

            if (suitCount[card.suit] >= 2)
                return true;
        }

        return false;

    }
    #endregion
}
