using System.Collections.Generic;

public class PlayerManager
{
    // 保管所有玩家的卡片列表。
    public List<CardList> players;

    // TODO:现在判断是否亮主/反主/暗主 这样的规则每次都要便利一边所有牌，所以可能可以使用下面的结构来优化，但是不是必须
    // Dictionary<Rank, Dictionary<Suit, int>> rankSuitCounter;

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

    public List<CardData> GetPlayerHand(int playerIndex)
    {
        return players[playerIndex].cardList;
    }
}