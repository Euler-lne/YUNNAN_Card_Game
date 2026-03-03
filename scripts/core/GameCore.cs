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

    public CardData DealOneCard(int seat)
    {
        CardData card = deckManager.DrawCard();
        playerManager.AddCardToPlayer(seat, card);
        return card;
    }
    /// <summary>
    /// 检查某个玩家当前手牌是否可以叫主
    /// </summary>
    /// <param name="playerId">服务器视角下的玩家座位ID</param>
    /// <returns>玩家可以选择的叫主方式，如果不能叫主返回 DeclareOption.NONE</returns>
    public DeclareOption CheckIfSeatCanDeclare(int playerId)
    {
        var hand = playerManager.GetPlayerHand(playerId);
        var trumpState = gameData.TrumpState;
        var currentLevel = gameData.GetCurrentRank();

        DeclareOption option = RuleEngine.GetDeclareOption(hand, trumpState, currentLevel);
        return option;
    }

}