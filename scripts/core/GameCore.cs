using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Euler.Global;
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
    #region 出牌相关
    public void RemoveCardFromPlayer(int playerIndex, List<CardData> cardDatas)
    {
        CardList cardList = playerManager.players[playerIndex];
        cardList.RemoveCard(cardDatas);
    }
    public List<int> GetSuitCards(int seat, List<CardData> cardDatas, Suit suit)
    {
        if (cardDatas.Count == 0)
        {
            return [];
        }
        List<CardData> suitCards = suit switch
        {
            Suit.SPADE => playerManager.players[seat].spadeList,
            Suit.CLUB => playerManager.players[seat].clubList,
            Suit.DIAMOND => playerManager.players[seat].diamondList,
            Suit.HEART => playerManager.players[seat].heartList,
            Suit.NONE => playerManager.players[seat].mainList,
            _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null)
        };

        return [.. CardData.Serialize(suitCards)];
    }

    public bool IsFinalTurn()
    {
        return playerManager.players[0].cardList.Count == 0;
    }
    #endregion

    public void ContinueGame()
    {

    }
    # region 甩牌相关
    public bool IsBiggest(List<CardData> cardDatas, Suit suit, int seat)
    {
        if (cardDatas.Count == 0)
        {
            GD.Print("GameCore中IsBiggest中传入了一个长度为0的列表");
            return false;
        }
        // 判断cardDatas是否在playerCardData中最大
        for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
        {
            List<CardData> playerCardData = suit switch
            {
                Suit.SPADE => playerManager.players[i].spadeList,
                Suit.CLUB => playerManager.players[i].clubList,
                Suit.DIAMOND => playerManager.players[i].diamondList,
                Suit.HEART => playerManager.players[i].heartList,
                Suit.NONE => playerManager.players[i].mainList,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null)
            };
            if (i == seat) continue;
            if (!RuleEngine.IsFirstGreater([.. CardData.Serialize(cardDatas)], [.. CardData.Serialize(playerCardData)]))
            {
                GD.Print($"座位{i}的卡牌比当前的值大");
                return false;
            }
        }
        return true;
    }
    #endregion

    #region 发牌
    public CardData DealOneCard(int seat)
    {
        CardData card = deckManager.DrawCard();
        playerManager.AddCardToPlayer(seat, card);
        return card;
    }
    #endregion
    #region 抠底
    public List<CardData> GetRestCard()
    {
        if (tableCards.Count != 0) return tableCards;
        tableCards = deckManager.GetRestCard();
        return tableCards;
    }
    public void DealerGetCard(int seat)
    {
        List<CardData> cardDatas = GetRestCard();
        foreach (var cardData in cardDatas)
            playerManager.AddCardToPlayer(seat, cardData);
    }
    public void DealRemoveCard(List<CardData> cardDatas)
    {
        tableCards = cardDatas;
        playerManager.RemoveCardFromPlayer(GetDealerSeat(), cardDatas);
    }
    public List<CardData>[] RegenerateCardList()
    {
        Rank rank = GetCurrentRank(GetDealerSeat() % GameSettings.PLAYER_COUNT);
        Suit suit = GetTrumpSuit();
        return playerManager.RegenerateCardList(suit, rank);
    }
    #endregion

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

    #region 工具函数
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
    public void SetCurrentGamePhase(GamePhase gamePhase)
    {
        gameData.CurrentPhase = gamePhase;
    }

    public void SetDealerSeat(int seat)
    {
        gameData.DealerSeat = seat;
    }
    public Rank GetCurrentRank(int seat)
    {
        return gameData.GetCurrentRank()[GameData.GetTeamIndex(seat)];
    }

    public List<Rank> GetCurrentRank()
    {
        return gameData.GetCurrentRank();
    }
    public int GetLeftCardNum()
    {
        return deckManager.GetRemainingCount();
    }
    public bool IsTrumpLocked()
    {
        return gameData.TrumpState.isLocked;
    }
    public void PrintDeclareInfo()
    {
        gameData.TrumpState.Print();
    }
    public bool IsDealer(int seat)
    {
        if (gameData.DealerSeat == seat) return true;
        if ((seat + 2) % GameSettings.PLAYER_COUNT == gameData.DealerSeat) return true;
        return false;
    }
    public void InscreaseIdlePlayerScore(int increase)
    {
        gameData.IdlePlayerScore += increase;
    }
    public void RemoveCardFrom(int seat, List<CardData> cardDatas)
    {
        // 如果cardDatas为空就删除所有的
        playerManager.RemoveCardFromPlayer(seat, cardDatas);
    }
    #endregion

}