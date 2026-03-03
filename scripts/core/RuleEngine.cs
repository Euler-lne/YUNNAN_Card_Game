using System;
using System.Collections.Generic;
using Godot;

public static class RuleEngine
{

    #region 发牌定主
    public static DeclareOption GetDeclareOption(
        List<CardData> hand,
        TrumpState trumpState,
        Rank currentLevel)
    {
        if (trumpState.isLocked)
            return DeclareOption.NONE;

        // 还没有主
        if (trumpState.trumpSuit == TrumpSuit.UNKNOW_TRUMP && !trumpState.haveTrump)
        {
            if (HasRankCard(hand, currentLevel))
                return DeclareOption.BRIGHTTRUMP;

            if (hand.Count == 1 && hand[0].rank == currentLevel) // 只有第一轮可以暗主
                return DeclareOption.DARKTRUMP;
        }
        else if (CanCounterTrump(hand, trumpState, currentLevel) && trumpState.haveTrump)
            return DeclareOption.COUNTERTRUMP;  // 有主的情况下才能反


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
            if (TrumpState.ToTrumpSuit(card.suit) == trumpState.trumpSuit)
                GD.Print($"当前型号和亮主的一样，不可以进行反主{card.suit}");
            if (card.rank != currentLevel || TrumpState.ToTrumpSuit(card.suit) == trumpState.trumpSuit)
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
}
