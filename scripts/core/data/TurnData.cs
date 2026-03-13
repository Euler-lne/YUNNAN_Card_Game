using System;
using System.Collections.Generic;

public class TurnData
{
    public PlayType playType;
    public Suit suit;
    public List<CardData> cardDatas = [];

    public TurnData(PlayType playType, Suit suit, List<CardData> cardDatas)
    {
        this.playType = playType;
        this.suit = suit;
        this.cardDatas = cardDatas ?? new List<CardData>();
    }

    public TurnData()
    {
        playType = PlayType.NONE;
        suit = Suit.NONE;
        cardDatas.Clear();
    }

    public int PlayNum() => cardDatas.Count;

    public static int[] Serialize(TurnData turnData)
    {
        var result = new List<int>
        {
            // 将 playType 和 suit 合并到一个 int（各占8位，足够容纳枚举值）
            ((int)turnData.playType << 8) | (int)turnData.suit,
            turnData.cardDatas.Count
        };
        foreach (var card in turnData.cardDatas)
            result.Add(CardData.Serialize(card));
        return [.. result];
    }

    public static TurnData Deserialize(int[] data)
    {
        if (data == null || data.Length < 2)
            throw new ArgumentException("Invalid data");

        int header = data[0];
        PlayType playType = (PlayType)(header >> 8);
        Suit suit = (Suit)(header & 0xFF);
        int count = data[1];
        var cardDatas = new List<CardData>(count);
        for (int i = 0; i < count; i++)
            cardDatas.Add(CardData.Deserialize(data[2 + i]));
        return new TurnData(playType, suit, cardDatas);
    }
}