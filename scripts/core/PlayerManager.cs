using System.Collections.Generic;

public class PlayerManager
{
    // 保管所有玩家的卡片列表。
    public List<CardList> players;

    public PlayerManager()
    {
        players = [];

        for (int i = 0; i < 4; i++)
        {
            players.Add(new CardList());
        }
    }

    public void AddCardToPlayer(int playerIndex, CardData card)
    {
        players[playerIndex].Insert(card);
    }

    public void RemoveCardFromPlayer(int playerIndex, List<CardData> cardDatas)
    {
        CardList cardList = players[playerIndex];
        cardList.RemoveCard(cardDatas);
    }

    public List<CardData> GetPlayerHand(int playerIndex)
    {
        return players[playerIndex].cardList;
    }
}