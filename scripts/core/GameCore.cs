using System.Collections.Generic;
using Godot;

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
        // deckManager.TestCreateDeck();

        gameData.CurrentPhase = GamePhase.DEALING;

        //TODO: 扣底
    }

    public void ContinueGame()
    {

    }

    public int GetDealerSeat() => gameData.DealerSeat;

    public GamePhase GetCurrentGamePhase() => gameData.CurrentPhase;

    public CardData DealOneCard(int seat)
    {
        CardData card = deckManager.DrawCard();
        playerManager.AddCardToPlayer(seat, card);
        return card;
    }
    public bool IsTrumpLocked()
    {
        return gameData.TrumpState.isLocked;
    }
    public void PrintDeclareInfo()
    {
        gameData.TrumpState.Print();
    }

    public void RemoveCardFrom(int seat, List<CardData> cardDatas)
    {
        // 如果cardDatas为空就删除所有的
        playerManager.RemoveCardFromPlayer(seat, cardDatas);
        NetworkManager.Instance.RemovePlayerCards(seat, cardDatas);
    }

    #region 叫主相关
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
    public bool CheckIfSeatCanDeclareOption(int playerId, DeclareOption option)
    {
        var hand = playerManager.GetPlayerHand(playerId);
        var trumpState = gameData.TrumpState;
        var currentLevel = gameData.GetCurrentRank();

        return RuleEngine.CanDeclareOfOption(hand, trumpState, currentLevel, option);
    }
    /// <summary>
    /// 设置主花色/叫主类型
    /// </summary>
    /// <param name="option">叫主类型</param>
    /// <param name="suit">玩家选择的花色</param>
    /// <param name="logicalSeat">叫主玩家座位</param>
    public void SetTrump(DeclareOption option, Suit suit, int logicalSeat)
    {
        GD.Print($"设置主花色: 玩家 {logicalSeat}, 类型 {option}, 花色 {suit}");

        // 庄家是谁
        gameData.TrumpState.dealerSeat = logicalSeat;

        // 判断是否无主
        gameData.TrumpState.haveTrump = true; // 或者根据规则改

        // 定主/锁定
        gameData.TrumpState.isLocked = option == DeclareOption.COUNTER_TRUMP;

        // 主花色
        if (option == DeclareOption.DARK_TRUMP)
            gameData.TrumpState.trumpSuit = TrumpSuit.UNKNOW_TRUMP;
        else
            gameData.TrumpState.trumpSuit = TrumpState.ToTrumpSuit(suit);
    }
    #endregion
    public Rank GetCurrentRank()
    {
        return gameData.GetCurrentRank();
    }
}