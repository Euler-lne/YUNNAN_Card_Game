using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class SelectedHandComposition
{
    public List<SingleCard> Singles { get; private set; }
    public List<DoubleCard> Doubles { get; private set; }
    public List<Tractor> Tractors { get; private set; }

    public List<Tractor> GetTratorsLenGreat(int len)
    {
        List<Tractor> tractors = [];
        foreach (var item in Tractors)
        {
            if (item.GetCount() >= len)
            {
                tractors.Add(item);
            }
        }
        return tractors;
    }

    public Tractor GetLargestTractor()
    {
        List<int> len = [];
        int maxLen = -1;
        for (int i = 0; i < Tractors.Count; i++)
        {
            len.Add(Tractors[i].GetCount());
            if (maxLen < Tractors[i].GetCount())
                maxLen = Tractors[i].GetCount();
        }
        for (int i = Tractors.Count - 1; i >= 0; i--)
        {
            if (Tractors[i].GetCount() == maxLen)
                return Tractors[i];
        }
        return null;
    }

    public DoubleCard GetLargestDouble()
    {
        if (Doubles.Count == 0)
            return null;
        return Doubles[^1];
    }

    public int GetLargestSingleCardValue()
    {
        int maxValue = -1;
        if (Singles.Count != 0)
            maxValue = RuleEngine.GetCardValue(CardData.Deserialize(Singles[^1].Card));
        if (Doubles.Count != 0)
        {
            int doubleMax = RuleEngine.GetCardValue(CardData.Deserialize(Doubles[^1].Card1));
            if (maxValue < doubleMax)
                maxValue = doubleMax;
        }
        if (Tractors.Count != 0)
        {
            int tractorMax = Tractors[^1].BiggestValue();
            if (maxValue < tractorMax)
                maxValue = tractorMax;
        }
        return maxValue;
    }

    public SelectedHandComposition(List<int> selectedIds)
    {
        if (selectedIds == null || selectedIds.Count == 0)
        {
            Singles = [];
            Doubles = [];
            Tractors = [];
            return;
        }

        // 按ID分组，得到每个ID的出现次数
        var groups = selectedIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.ToList());

        // 分离单张ID（出现1次）和对子ID（出现2次）
        var singleIds = new List<int>();
        var pairIds = new List<int>();
        foreach (var kv in groups)
        {
            if (kv.Value.Count == 1)
                singleIds.Add(kv.Key);
            else if (kv.Value.Count == 2)
                pairIds.Add(kv.Key);
        }

        // 计算每个对子ID的牌值，并按值排序
        var pairValues = pairIds.Select(id => new
        {
            Id = id,
            Value = RuleEngine.GetCardValue(CardData.Deserialize(id))
        })
        .OrderBy(x => x.Value)
        .ToList();

        // 找出连续值段（相邻差 <= 1），构成拖拉机
        var tractorSegments = new List<List<int>>();
        if (pairValues.Count >= 2)
        {
            var currentSegment = new List<int> { pairValues[0].Id };
            for (int i = 1; i < pairValues.Count; i++)
            {
                if (pairValues[i].Value - pairValues[i - 1].Value <= 1)
                {
                    currentSegment.Add(pairValues[i].Id);
                }
                else
                {
                    if (currentSegment.Count >= 2)
                        tractorSegments.Add([.. currentSegment]);
                    currentSegment.Clear();
                    currentSegment.Add(pairValues[i].Id);
                }
            }
            if (currentSegment.Count >= 2)
                tractorSegments.Add(currentSegment);
        }

        // 从 pairIds 中移除已被拖拉机占用的 ID
        var usedPairIds = tractorSegments.SelectMany(seg => seg).ToHashSet();
        var remainingPairIds = pairIds.Where(id => !usedPairIds.Contains(id)).ToList();

        // 构建结果
        Singles = [];
        Doubles = [];
        Tractors = [];

        // 处理单张
        foreach (var id in singleIds)
        {
            Singles.Add(new SingleCard { Card = id });
        }

        // 处理剩余对子（普通对子）
        foreach (var id in remainingPairIds)
        {
            Doubles.Add(new DoubleCard { Card1 = id, Card2 = id }); // 同一CardData代表两张相同牌
        }

        // 处理拖拉机段
        foreach (var seg in tractorSegments)
        {
            var pairs = new List<DoubleCard>();
            foreach (var id in seg)
            {
                var card = CardData.Deserialize(id);
                pairs.Add(new DoubleCard { Card1 = id, Card2 = id });
            }
            Tractors.Add(new Tractor { Pairs = pairs });
        }
        // 排序 Singles（按牌值升序）
        Singles = [.. Singles.OrderBy(s => RuleEngine.GetCardValue(CardData.Deserialize(s.Card)))];

        // 排序 Doubles（按牌值升序）
        Doubles = [.. Doubles.OrderBy(d => RuleEngine.GetCardValue(CardData.Deserialize(d.Card1)))];

        // Tractors 已有序，无需额外排序
    }


}
public class SingleCard
{
    public int Card { get; set; }
}

public class DoubleCard
{
    public int Card1 { get; set; }
    public int Card2 { get; set; }

    public int GetCardValue() => RuleEngine.GetCardValue(CardData.Deserialize(Card1));
}

public class Tractor
{
    public List<DoubleCard> Pairs { get; set; }
    public int GetCount() => Pairs.Count;
    public int BiggestValue()
    {
        if (Pairs.Count == 0) return -1;
        return RuleEngine.GetCardValue(CardData.Deserialize(Pairs[^1].Card1));
    }

    public int SmallesValue()
    {
        if (Pairs.Count == 0) return -1;
        return RuleEngine.GetCardValue(CardData.Deserialize(Pairs[0].Card1));
    }

    public static bool IsFirstGreater(Tractor tractor1, Tractor tractor2)
    {
        if (tractor1.GetCount() > tractor2.GetCount()) return true;
        if (tractor1.BiggestValue() > tractor2.BiggestValue()) return true;
        return false;
    }
}
