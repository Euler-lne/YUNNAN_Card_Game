using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Godot;
using GodotPlugins.Game;
public class CardData
{
    public Suit suit;
    public Rank rank;

    public CardData(Suit suit, Rank rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

    public static int Serialize(CardData card)
    {
        return (int)card.suit * 100 + (int)card.rank;
    }

    public static int[] Serialize(List<CardData> hand)
    {
        int[] ids = new int[hand.Count];

        for (int i = 0; i < hand.Count; i++)
            ids[i] = Serialize(hand[i]); // 你自己定义的唯一ID
        return ids;
    }

    public static List<CardData> Deserialize(int[] ids)
    {
        List<CardData> list = [];

        foreach (var id in ids)
            list.Add(Deserialize(id)); // 你自己的查表方法

        return list;
    }

    public static bool IsCardDataInList(CardData cardData, List<CardData> cardDatas)
    {
        foreach (var item in cardDatas)
        {
            if (Serialize(cardData) == Serialize(item))
                return true;
        }
        return false;
    }

    public static CardData Deserialize(int id)
    {
        Suit suit = (Suit)(id / 100);
        Rank rank = (Rank)(id % 100);

        return new CardData(suit, rank);
    }

    public bool IsPoint()
    {
        return rank == Rank.FIVE || rank == Rank.TEN || rank == Rank.KING;
    }

}

public class CardList
{
    public List<CardData> spadeList, heartList, clubList, diamondList, mainList, cardList;

    public CardList()
    {
        spadeList = [];
        heartList = [];
        clubList = [];
        diamondList = [];
        mainList = [];
        cardList = [];
    }

    public void GenarateCardList(Suit suit, Rank rank, List<CardData> cardDatas = null)
    {
        spadeList.Clear();
        heartList.Clear();
        clubList.Clear();
        diamondList.Clear();
        mainList.Clear();
        if (cardDatas != null)
            cardList = cardDatas;
        // 遍历 cardList 的副本，避免修改原集合导致异常
        RuleEngine.UpdateCardValues(suit, rank);
        foreach (CardData cardData in cardList.ToList())
        {
            Insert(cardData, suit, rank);
        }
        GenarateCardList();
    }

    private void GenarateCardList()
    {
        cardList.Clear();
        foreach (var item in spadeList)
            cardList.Add(item);
        foreach (var item in heartList)
            cardList.Add(item);
        foreach (var item in clubList)
            cardList.Add(item);
        foreach (var item in diamondList)
            cardList.Add(item);
        foreach (var item in mainList)
            cardList.Add(item);
    }

    public void Insert(CardData cardData)
    {
        switch (cardData.suit)
        {
            case Suit.SPADE:  // 黑桃
                InsertFrom(spadeList, cardData);
                break;
            case Suit.HEART:  // 红心
                InsertFrom(heartList, cardData);
                break;
            case Suit.CLUB:  // 方片
                InsertFrom(clubList, cardData);
                break;
            case Suit.DIAMOND:  // 梅花
                InsertFrom(diamondList, cardData);
                break;
            case Suit.NONE:  // 大小王
                InsertFrom(mainList, cardData);
                break;
        }
    }

    private void Insert(CardData cardData, Suit suit, Rank rank)
    {
        if (cardData.rank == rank || cardData.suit == suit)
        {
            // 为主牌
            InsertFrom(mainList, cardData, suit, rank);
            return;
        }

        switch (cardData.suit)
        {
            case Suit.SPADE:  // 黑桃
                if (cardData.rank == Rank.ACE)// 常主牌
                    InsertFrom(mainList, cardData, suit, rank);
                else
                    InsertFrom(spadeList, cardData);
                break;
            case Suit.HEART:  // 红心
                InsertFrom(heartList, cardData);
                break;
            case Suit.CLUB:  // 方片
                InsertFrom(clubList, cardData);
                break;
            case Suit.DIAMOND:  // 梅花
                InsertFrom(diamondList, cardData);
                break;
            case Suit.NONE:  // 大小王
                InsertFrom(mainList, cardData, suit, rank);
                break;
        }
    }

    private void InsertFrom(List<CardData> cardDatas, CardData cardData)
    {
        int value = (int)cardData.rank;
        int l = 0, r = cardDatas.Count;
        while (l < r)
        {
            int mid = (l + r) >> 1;
            if ((int)cardDatas[mid].rank < value)
                l = mid + 1;
            else
                r = mid;
        }
        cardDatas.Insert(l, cardData);
        GenarateCardList();
    }
    private void InsertFrom(List<CardData> cardDatas, CardData cardData, Suit trumpSuit, Rank trumpRank)
    {
        int value = RuleEngine.GetCardValue(cardData);
        int l = 0, r = cardDatas.Count;
        while (l < r)
        {
            int mid = (l + r) >> 1;
            CardData midCardData = cardDatas[mid];
            int midValue = RuleEngine.GetCardValue(midCardData);
            if (midValue < value)
                l = mid + 1;
            else
                r = mid;
        }
        cardDatas.Insert(l, cardData);
    }

    public void ClearAllList()
    {
        spadeList.Clear();
        heartList.Clear();
        clubList.Clear();
        diamondList.Clear();
        mainList.Clear();
        cardList.Clear();
    }

    private void RemoveCard(CardData cardData)
    {
        RemoveFrom(mainList, cardData); // 现在这里移除
        switch (cardData.suit)
        {
            case Suit.SPADE:  // 黑桃
                RemoveFrom(spadeList, cardData);
                break;
            case Suit.HEART:  // 红心
                RemoveFrom(heartList, cardData);
                break;
            case Suit.CLUB:  // 梅花
                RemoveFrom(clubList, cardData);

                break;
            case Suit.DIAMOND:  // 方片
                RemoveFrom(diamondList, cardData);

                break;
            case Suit.NONE:
                break;
        }
    }
    public void RemoveCard(List<CardData> cardDatas)
    {

        if (cardDatas.Count == 0)
            ClearAllList();
        else
        {
            foreach (var cardData in cardDatas)
            {
                RemoveCard(cardData);
            }
            GenarateCardList();
        }
    }

    private void RemoveFrom(List<CardData> cardDatas, CardData cardData)
    {
        int targetId = CardData.Serialize(cardData);
        int index = -1;

        for (int i = 0; i < cardDatas.Count; i++)
        {
            if (CardData.Serialize(cardDatas[i]) == targetId)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
            cardDatas.RemoveAt(index);


    }
}