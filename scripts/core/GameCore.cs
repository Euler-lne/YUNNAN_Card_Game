using System.Collections.Generic;
using Godot;

public class GameCore
{
    private readonly DeckManager deckManager;
    private readonly PlayerManager playerManager;

    private List<CardData> tableCards = [];// 压底的牌

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
    }

    public void ContinueGame()
    {

    }

    public int GetDealerSeat() => gameData.DealerSeat;

    public bool IsSnatchDealer() => gameData.IsSnatchDealer;

    public void SetSnatchDealer(bool value)
    {
        gameData.IsSnatchDealer = value;
    }

    public bool IsSameRank()
    {
        List<Rank> ranks = gameData.GetCurrentRank();
        return ranks[0] == ranks[1];
    }

    public GamePhase GetCurrentGamePhase() => gameData.CurrentPhase;

    public void SetDealerSeat(int seat)
    {
        // TODO:在这个函数通知UI显示当前的DealerSeat
        gameData.DealerSeat = seat;
    }


    public CardData DealOneCard(int seat)
    {
        CardData card = deckManager.DrawCard();
        playerManager.AddCardToPlayer(seat, card);
        return card;
    }

    public List<CardData> GetRestCard()
    {
        if (tableCards.Count != 0) return tableCards;
        tableCards = deckManager.GetRestCard();
        return tableCards;
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
    }

    #region 叫主相关
    public DeclareOption CheckIfSeatCanDeclare(int playerId)
    {
        var hand = playerManager.GetPlayerHand(playerId);
        var trumpState = gameData.TrumpState;
        var currentLevel = gameData.GetCurrentRank()[GameData.GetTeamIndex(playerId)];

        DeclareOption option = RuleEngine.GetDeclareOption(hand, trumpState, currentLevel);
        return option;
    }
    public bool CheckIfSeatCanDeclareOption(int playerId, DeclareOption option)
    {
        var hand = playerManager.GetPlayerHand(playerId);
        var trumpState = gameData.TrumpState;
        var currentLevel = gameData.GetCurrentRank()[GameData.GetTeamIndex(playerId)];

        return RuleEngine.CanDeclareOfOption(hand, trumpState, currentLevel, option);
    }
    public void SetTrump(DeclareOption option, Suit suit)
    {
        // 判断是否无主
        gameData.TrumpState.haveTrump = true; // 或者根据规则改

        // 定主/锁定
        gameData.TrumpState.isLocked = option == DeclareOption.COUNTER_TRUMP;

        // 主花色
        gameData.TrumpState.trumpSuit = suit;
    }

    public bool HaveTrump() => gameData.TrumpState.haveTrump;

    public Suit GetTrumpSuit() => gameData.TrumpState.trumpSuit;

    public void LockSuit(Suit suit) { SetTrump(DeclareOption.COUNTER_TRUMP, suit); }
    #endregion
    public Rank GetCurrentRank(int seat)
    {
        return gameData.GetCurrentRank()[GameData.GetTeamIndex(seat)];
    }

    public List<Rank> GetCurrentRank()
    {
        return gameData.GetCurrentRank();
    }
}