using System.Collections.Generic;
using System.Linq;
using Godot;

public static class RuleEngine
{
    // 缓存：键为牌的序列化ID，值为牌力值（由 GetCardValue 计算）
    private static Dictionary<int, int> cardValueCache = [];

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
    #endregion

    #region 出牌判断


    public static bool IsSameSuit(List<int> selectedCards, CardData trumpCardData)
    {
        if (selectedCards.Count <= 1) return true;
        CardData firstCardData = CardData.Deserialize(selectedCards[0]);
        Suit suit = GetSuit(firstCardData, trumpCardData);
        foreach (int id in selectedCards)
        {
            if (GetSuit(CardData.Deserialize(id), trumpCardData) != suit)
                return false;
        }
        return true;
    }

    public static Suit GetSuit(CardData cardData, CardData trumpCardData)
    {
        if (cardData.rank == trumpCardData.rank || cardData.suit == trumpCardData.suit)
            return Suit.NONE;
        if (cardData.suit == Suit.NONE) // 大小鬼
            return Suit.NONE;
        if (cardData.suit == Suit.SPADE && cardData.rank == Rank.ACE) // 常主
            return Suit.NONE;
        return cardData.suit;
    }

    public static PlayType DetermineSelectedPlayType(List<int> selectedCards)
    {
        if (selectedCards.Count == 0) return PlayType.NONE;
        if (selectedCards.Count == 1) return PlayType.SINGLE;
        if (selectedCards.Count == 2)
        {
            Rank first = CardData.Deserialize(selectedCards[0]).rank;
            Rank second = CardData.Deserialize(selectedCards[1]).rank;
            if (first == second) return PlayType.DOUBLE;
        }
        if (IsTractor(selectedCards))
            return PlayType.EVEN_CORRECT;
        return PlayType.THROW_CARD;
    }
    public static List<int> GetCardsOfType(List<int> hand, PlayType type)
    {
        if (hand == null || hand.Count == 0)
            return [];

        // 按ID分组，得到每个ID的张数（两副牌中最多2张）
        var groups = hand.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        switch (type)
        {
            case PlayType.SINGLE:
                // 返回所有出现次数为1的牌
                return [.. groups.Where(kvp => kvp.Value == 1).SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value))];

            case PlayType.DOUBLE:
                // 返回所有出现次数为2的牌
                return [.. groups.Where(kvp => kvp.Value == 2).SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value))];

            case PlayType.EVEN_CORRECT:
                // 找出所有出现次数为2的ID（即对子）
                var pairIds = groups.Where(kvp => kvp.Value == 2).Select(kvp => kvp.Key).ToList();
                if (pairIds.Count < 2)
                    return [];

                // 计算每个ID的牌值，并按值排序
                var pairValues = pairIds.Select(id => new
                {
                    Id = id,
                    Value = GetCardValue(CardData.Deserialize(id))
                })
                .OrderBy(x => x.Value)
                .ToList();

                // 找出连续的值段（相邻差 <= 1，允许相等）
                List<List<int>> tractorSegments = [];
                List<int> currentSegment = [pairValues[0].Id];
                for (int i = 1; i < pairValues.Count; i++)
                {
                    // 如果当前值与上一个值的差值 <= 1，则属于同一连续段
                    if (pairValues[i].Value - pairValues[i - 1].Value <= 1)
                    {
                        currentSegment.Add(pairValues[i].Id);
                    }
                    else
                    {
                        // 否则结束当前段，如果段长度 >=2 则记录
                        if (currentSegment.Count >= 2)
                            tractorSegments.Add([.. currentSegment]);
                        // 开始新段
                        currentSegment.Clear();
                        currentSegment.Add(pairValues[i].Id);
                    }
                }
                // 处理最后一个段
                if (currentSegment.Count >= 2)
                    tractorSegments.Add(currentSegment);

                // 收集所有拖拉机段中的牌（每个ID取两张）
                var result = new List<int>();
                foreach (var seg in tractorSegments)
                {
                    foreach (var id in seg)
                    {
                        result.Add(id);
                        result.Add(id); // 每对两张
                    }
                }
                return result;

            default:
                return [];
        }
    }

    private static bool IsTractor(List<int> selectedCards)
    {
        if (selectedCards.Count < 4 || selectedCards.Count % 2 != 0)
            return false;

        // 按 ID 分组（ID 由花色和点数唯一确定）
        var groups = selectedCards.GroupBy(id => id).ToList();

        // 每组必须恰好两张（对子）
        if (groups.Any(g => g.Count() != 2)) return false;
        // 至少两个不同的组（即至少两个不同的花色点数组合）
        if (groups.Count < 2) return false;

        // 获取每个组的牌值（取组内任意一张）
        var values = groups.Select(g => GetCardValue(CardData.Deserialize(g.Key)))
                           .OrderBy(v => v).ToList();


        // 否则检查值是否连续（相邻差1）
        for (int i = 0; i < values.Count - 1; i++)
        {
            if (values[i + 1] - values[i] != 1 && values[i + 1] != values[i])
                return false;
        }
        return true;
    }
    #endregion

    #region 计算牌力值函数

    public static void UpdateCardValues(Suit trumpSuit, Rank trumpRank)
    {
        cardValueCache.Clear();
        // GD.Print($"当前主花色{trumpSuit} 当前主等级{trumpRank}");
        // 生成所有牌（54张）
        foreach (Suit suit in new[] { Suit.SPADE, Suit.HEART, Suit.CLUB, Suit.DIAMOND })
        {
            for (Rank r = Rank.TWO; r <= Rank.ACE; r++)
            {
                CardData cd = new(suit, r);
                int id = CardData.Serialize(cd);
                cardValueCache[id] = ComputeCardValue(cd, trumpSuit, trumpRank);
                // GD.Print($"花色{suit} 点数{r} 排名{cardValueCache[id]}");
            }
        }
        // 大小王
        cardValueCache[CardData.Serialize(new CardData(Suit.NONE, Rank.SMALL_JOKER))] = ComputeCardValue(new CardData(Suit.NONE, Rank.SMALL_JOKER), trumpSuit, trumpRank);
        cardValueCache[CardData.Serialize(new CardData(Suit.NONE, Rank.BIG_JOKER))] = ComputeCardValue(new CardData(Suit.NONE, Rank.BIG_JOKER), trumpSuit, trumpRank);
        // GD.Print($"小王{cardValueCache[CardData.Serialize(new CardData(Suit.NONE, Rank.SMALL_JOKER))]}");
        // GD.Print($"大王{cardValueCache[CardData.Serialize(new CardData(Suit.NONE, Rank.BIG_JOKER))]}");


    }

    private static int ComputeCardValue(CardData cardData, Suit trumpSuit, Rank trumpRank)
    {
        // 此处直接复制您现有的 GetCardValue 方法体（包括两个分支和辅助方法）
        // 注意：辅助方法 GetSuitRankIndex 也需改为静态方法（或内联）
        if (trumpRank == Rank.ACE && trumpSuit == Suit.SPADE)
        {
            if (cardData.suit == trumpSuit && cardData.rank == Rank.FIVE) return 20;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.BIG_JOKER) return 19;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.SMALL_JOKER) return 18;
            else if (cardData.suit == Suit.SPADE && cardData.rank == Rank.ACE) return 17; // 主牌也是
            else if (cardData.rank == trumpRank) return 16;
            return GetSuitRankIndex(cardData.rank, cardData.suit, trumpSuit, trumpRank, 15);
        }
        else if (trumpRank != Rank.FIVE)
        {
            if (cardData.suit == trumpSuit && cardData.rank == Rank.FIVE) return 20;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.BIG_JOKER) return 19;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.SMALL_JOKER) return 18;
            else if (cardData.suit == Suit.SPADE && cardData.rank == Rank.ACE) return 17;
            else if (cardData.suit == trumpSuit && cardData.rank == trumpRank) return 16;
            else if (cardData.rank == trumpRank) return 15;
            return GetSuitRankIndex(cardData.rank, cardData.suit, trumpSuit, trumpRank, 14);
        }
        else
        {
            if (cardData.suit == trumpSuit && cardData.rank == Rank.FIVE) return 20;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.BIG_JOKER) return 19;
            else if (cardData.suit == Suit.NONE && cardData.rank == Rank.SMALL_JOKER) return 18;
            else if (cardData.suit == Suit.SPADE && cardData.rank == Rank.ACE) return 17;
            else if (cardData.rank == trumpRank) return 16;
            return GetSuitRankIndex(cardData.rank, cardData.suit, trumpSuit, trumpRank, 15);
        }
    }

    // 辅助方法保持不变（但建议也内联或作为静态私有方法）
    private static int GetSuitRankIndex(Rank rank, Suit suit, Suit trumpSuit, Rank trumpRank, int start)
    {
        List<Rank> remaining = new List<Rank>();
        for (Rank r = Rank.ACE; r >= Rank.TWO; r--)
        {
            if (suit == trumpSuit && r == Rank.FIVE) continue;
            if (r == trumpRank) continue; // 正主和副主
            if (suit == Suit.SPADE && r == Rank.ACE) continue;
            remaining.Add(r);
        }
        int index = remaining.IndexOf(rank);
        return start - index;
    }

    public static int GetCardValue(CardData cardData)
    {
        int id = CardData.Serialize(cardData);
        return cardValueCache.TryGetValue(id, out int value) ? value : -1; // -1 表示未初始化或错误
    }
    #endregion

}
