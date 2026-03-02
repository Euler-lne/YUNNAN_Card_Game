using System.Collections.Generic;
using Godot;

public class GameCore
{
    private DeckManager deckManager;
    private PlayerManager playerManager;

    public void StartGame()
    {
        deckManager = new DeckManager();
        playerManager = new PlayerManager();

        deckManager.CreateDeck();
        deckManager.Shuffle();

        DealCards();

        //TODO: 扣底
    }

    public List<CardData> GetPlayerHand(int playerId)
    {
        return playerManager.GetPlayerHand(playerId);
    }

    private void DealCards()
    {
        int currentPlayer = 0;

        while (deckManager.GetRemainingCount() > 8)
        {
            CardData card = deckManager.DrawCard();
            playerManager.AddCardToPlayer(currentPlayer, card);

            currentPlayer++;
            if (currentPlayer >= 4)
                currentPlayer = 0;
        }

        // GD.Print("发牌完成");
        playerManager.DealCardEnd();
        // for (int i = 0; i < 4; i++)
        // {
        //     GD.Print("玩家 " + i + " 手牌数量: " + playerManager.GetPlayerHand(i).Count);
        //     foreach (CardData item in playerManager.GetPlayerHand(i))
        //     {
        //         GD.Print("花色 " + item.suit + " 点数 " + item.rank);
        //     }
        // }
    }
}