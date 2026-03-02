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
        //TODO: 扣底
    }

    public List<CardData> GetPlayerHand(int playerId)
    {
        return playerManager.GetPlayerHand(playerId);
    }

    public CardData DrawCardForPlayer(int playerId)
    {
        CardData card = deckManager.DrawCard();
        playerManager.AddCardToPlayer(playerId, card);
        GD.Print($"玩家{playerId} 抽牌: {card}");
        return card;
    }

}