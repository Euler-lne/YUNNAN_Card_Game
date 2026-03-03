using System.Collections.Generic;

public class GameCore
{
    private readonly DeckManager deckManager;
    private readonly PlayerManager playerManager;

    private readonly GameData gameData;

    public GameCore()
    {
        deckManager = new DeckManager();
        playerManager = new PlayerManager();
        gameData = new GameData();
    }

    public void StartGame()
    {
        deckManager.CreateDeck();
        deckManager.Shuffle();

        gameData.CurrentPhase = GamePhase.DEALING;

        //TODO: 扣底
    }

    public void ContinueGame()
    {

    }

    public DealResult DealOneCard(int seat)
    {
        CardData card = deckManager.DrawCard();
        playerManager.AddCardToPlayer(seat, card);
        return new DealResult(card, playerManager.GetPlayerHand(seat));
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerId">服务器视角下的玩家座位ID</param>
    /// <returns></returns>
    public bool CheckIfSeatCanDeclare(int playerId)
    {
        RuleEngine.GetDeclareOption(playerManager.GetPlayerHand(playerId), gameData.TrumpState, gameData.GetCurrentRank());
        return false;
    }

}