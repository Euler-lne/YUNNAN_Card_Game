using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Euler.Event;
using Euler.Global;
using Godot;

public class TurnManager
{
    private TurnRequest turnRequest;

    private int dealer;
    private int currentSeat;

    private readonly List<CardData>[] playedCards = new List<CardData>[4];

    private TaskCompletionSource<bool> playTcs;

    private TurnData turnData;

    private CardData trumpCardData;

    private GameCore gameCore;
    private List<CardData> pointCards = [];
    public Action TurnOver;

    public void Init(TurnRequest turnRequest, int dealer, GameCore gameCore)
    {
        this.turnRequest = turnRequest;
        this.dealer = dealer;
        this.gameCore = gameCore;

        TurnEvent.PlayCardButtonPressEvent = OnPlayCardButtonPress;

        turnData = new();
    }

    public void SetFirstTurn()
    {
        trumpCardData = new(gameCore.GetTrumpSuit(), gameCore.GetCurrentRank(gameCore.GetDealerSeat()));
        pointCards = [];
    }

    public async void StartTurn(int dealer = -1)
    {
        if (dealer != -1)
        {
            this.dealer = dealer;
        }
        currentSeat = this.dealer;

        turnData = new();

        // GD.Print("StartTurn开始");

        await WaitPlayerPlayCard();

        // GD.Print("StartTurn开始调用递增");

        await IncreaseCurrentSeat();
    }

    private async Task IncreaseCurrentSeat()
    {
        currentSeat++;
        currentSeat %= GameSettings.PLAYER_COUNT;

        // GD.Print($"调用递增，当前是 {currentSeat}");

        if (currentSeat == dealer)
        {
            TurnEnd();
        }
        else
        {
            await NextOne();
        }
    }

    private async Task NextOne()
    {
        // GD.Print($"调用下一个人，当前是 {currentSeat}");

        await WaitPlayerPlayCard();

        // GD.Print("NextOne开始调用递增");

        await IncreaseCurrentSeat();
    }

    private async void TurnEnd()
    {
        GD.Print($"回合结束 当前seat={currentSeat}");

        int winner = GetWinnerSeat();
        GD.Print($"本轮赢家{winner}");
        dealer = winner;

        // 清空牌
        await WaitAsync(GameSettings.INFO_EXIST_TIME * 2); // 稍微等一下
        turnRequest.NewTurn(gameCore.IsDealer(dealer), dealer);
        if (!gameCore.IsDealer(dealer))
            AddPointCard();
        if (!gameCore.IsFinalTurn())
            StartTurn();
        else
        {
            TurnFinish(winner);
        }
    }

    private async void TurnFinish(int lastWinner)
    {
        await WaitAsync(GameSettings.MOVE_DURATION_TIME);  // 等待分卡移动到中心
        turnRequest.TurnOver();
        bool isDealerWin = gameCore.IsDealer(lastWinner); // 闲家赢了可以翻底牌
                                                          // 将卡牌闲家赢得的牌展开
        turnRequest.ExpandScoreCard(pointCards.Count);
        await WaitAsync(GameSettings.EXPAND_DURATION_TIME);
        for (int i = pointCards.Count - 1; i >= 0; i--)
        {
            CardData card = pointCards[i];
            turnRequest.MoveCardToScore(card);
            await WaitAsync(GameSettings.MOVE_DURATION_TIME);
            int increase = card.rank == Rank.FIVE ? 5 : 10;
            gameCore.InscreaseIdlePlayerScore(increase);
            pointCards.RemoveAt(i);
        }
        if (!isDealerWin)
        {
            // 翻出底牌，用双牌赢的话就*2，用单牌赢的话就不翻倍
            List<CardData> cardDatas = playedCards[lastWinner];
            SelectedHandComposition winnerCmp = new([.. CardData.Serialize(cardDatas)]);
            int times = 1;
            PlayType playType = winnerCmp.GetPlayType();
            if (playType == PlayType.DOUBLE)
                times = 2;
            else if (playType == PlayType.EVEN_CORRECT)
                times = winnerCmp.GetEvenCorrectLen();
            GD.Print($"闲家获胜准备翻倍数量{times}");
            List<CardData> tableCards = gameCore.GetRestCard();
            turnRequest.ExpandTableCard(tableCards);
            gameCore.InscreaseIdlePlayerScore(90);
            await WaitAsync(GameSettings.EXPAND_DURATION_TIME);
            for (int i = tableCards.Count - 1; i >= 0; i--)
            {
                CardData card = tableCards[i];
                if (!card.IsPoint()) continue;
                turnRequest.MoveCardToScore(card);
                await WaitAsync(GameSettings.MOVE_DURATION_TIME);
                int increase = card.rank == Rank.FIVE ? 5 : 10;
                gameCore.InscreaseIdlePlayerScore(increase * times);
                tableCards.RemoveAt(i);
            }
            turnRequest.ClearPointCards();
        }
        List<CardData> winnerCards = playedCards[lastWinner];
        int score = gameCore.GetIdleScore();
        ScoreResult scoreResult = GameCore.GetScoreResult(score);
        string info = $"闲家分数{score}";
        gameCore.WinRound(lastWinner, scoreResult, winnerCards);
        List<Rank> ranks = gameCore.GetCurrentRank();
        for (int i = 0; i < ranks.Count; i++)
        {
            turnRequest.ChangeLevel(i, ranks[i]);
        }
        turnRequest.SetInfo(-1, info);
        await WaitAsync(GameSettings.INFO_EXIST_TIME);
        TurnOver?.Invoke();
    }

    private async Task WaitPlayerPlayCard()
    {
        turnRequest.TurnStart(currentSeat, turnData, gameCore.IsDealer(currentSeat), trumpCardData);

        playTcs = new TaskCompletionSource<bool>();

        // GD.Print($"开始异步等待 playTcs hash={playTcs.GetHashCode()}");

        await playTcs.Task;

        // GD.Print($"结束异步等待 playTcs hash={playTcs.GetHashCode()}");

        playTcs = null;
    }

    private void OnPlayCardButtonPress(List<CardData> cardDatas)
    {
        if (playTcs == null)
        {
            // GD.Print("当前没有等待玩家出牌");
            return;
        }

        if (cardDatas.Count == 0)
        {
            // GD.Print("当前选择的牌数为0");
            return;
        }

        List<int> selectCards = [.. CardData.Serialize(cardDatas)];

        PlayType playType = RuleEngine.DetermineSelectedPlayType(selectCards);

        // GD.Print($"当前选择的牌的类型 {playType}");

        bool isValid;

        if (currentSeat == dealer)
        {
            isValid = DealerPlayCard(playType, cardDatas);
        }
        else
        {
            isValid = IdlePlayerPlayCard(cardDatas);
        }

        if (!isValid)
        {
            turnRequest.TurnStart(currentSeat, turnData, gameCore.IsDealer(currentSeat), trumpCardData);
            GD.Print("不合理，重新开始一轮");
            return;
        }
        gameCore.RemoveCardFrom(currentSeat, cardDatas);
        NetworkManager.Instance.PlayCard(
            currentSeat,
            cardDatas,
            false,
            gameCore.GetCurrentGamePhase()
        );

        playedCards[currentSeat] = cardDatas;

        if (currentSeat == dealer)
        {
            Suit suit = RuleEngine.GetSuit(cardDatas[0], trumpCardData);
            turnData = new(playType, playedCards[currentSeat].Count, suit);
        }

        turnRequest.TurnEnd(currentSeat);

        playTcs.TrySetResult(true);
    }

    private bool DealerPlayCard(PlayType playType, List<CardData> cardDatas)
    {
        bool isValid = true;

        if (playType == PlayType.THROW_CARD)
        {
            isValid = JudgeThrowCard(cardDatas);

            if (!isValid)
            {
                int increase = gameCore.IsDealer(currentSeat) ? 5 : -5;
                gameCore.InscreaseIdlePlayerScore(increase);
            }
        }

        return isValid;
    }

    private bool IdlePlayerPlayCard(List<CardData> cardDatas)
    {
        // 牌数不一致
        if (cardDatas.Count != turnData.playNum)
        {
            string info = $"当前手牌 {cardDatas.Count} 张，需要 {turnData.playNum} 张";
            turnRequest.SetInfo(currentSeat, info);
            return false;
        }
        // 同花色牌
        if (playedCards[dealer].Count == 0)
        {
            GD.PrintErr("出现问题，当前庄家没有出牌，就调用了闲家出牌逻辑");
            return false;
        }
        List<int> suitCards = gameCore.GetSuitCards(
            currentSeat,
            RuleEngine.GetSuit(playedCards[dealer][0], trumpCardData)
        );
        int suitNum = suitCards.Count;
        // 不够跟牌
        if (suitNum <= turnData.playNum)  // 判断是否有庄家的牌，花色是否够
            return true;
        // 结构分析
        SelectedHandComposition selectCmp = new([.. CardData.Serialize(cardDatas)]);
        SelectedHandComposition playerCmp = new(suitCards);
        SelectedHandComposition dealerCmp = new([.. CardData.Serialize(playedCards[dealer])]);

        return FollowDealerStructure(dealerCmp, selectCmp, playerCmp);
    }
    private bool FollowDealerStructure(SelectedHandComposition dealerCmp, SelectedHandComposition selectCmp,
                    SelectedHandComposition playerCmp)
    {
        // 玩家实际选牌
        bool hasTractorInSelect = selectCmp.Tractors.Count > 0;
        bool hasDoubleInSelect = selectCmp.Doubles.Count > 0;

        // 玩家手牌
        bool hasTractorInHand = playerCmp.Tractors.Count > 0;
        bool hasDoubleInHand = playerCmp.Doubles.Count > 0;

        // 庄家牌
        bool dealerHasTractor = dealerCmp.Tractors.Count > 0;
        bool dealerHasDouble = dealerCmp.Doubles.Count > 0;

        // ---------- 1 庄家出了连对 ----------
        if (dealerHasTractor)
        {
            if (hasTractorInHand && !hasTractorInSelect)
            {
                turnRequest.SetInfo(currentSeat, "请选择连对出牌");
                return false;
            }
            else if (!hasTractorInHand && hasDoubleInHand && !hasDoubleInSelect)
            {
                turnRequest.SetInfo(currentSeat, "请选择对子出牌");
                return false;
            }
        }
        // ---------- 2 庄家出了对子 ----------
        if (dealerHasDouble)
        {
            if (hasDoubleInHand && !hasDoubleInSelect)
            {
                turnRequest.SetInfo(currentSeat, "请选择对子出牌");
                return false;
            }
        }

        // ---------- 3 其他情况（手上没有对应牌型） ----------
        // 可以出单张，或者任意其他牌
        return true;
    }

    #region 工具函数
    private bool JudgeThrowCard(List<CardData> cardDatas)
    {
        Suit suit = RuleEngine.GetSuit(cardDatas[0], trumpCardData);

        return gameCore.IsBiggest(cardDatas, suit, currentSeat);
    }
    public async Task WaitAsync(float seconds)
    {
        await Task.Delay((int)(seconds * 1000));
    }
    private void AddPointCard()
    {
        for (int i = 0; i < playedCards.Length; i++)
        {
            foreach (var item in playedCards[i])
            {
                if (item.IsPoint())
                    pointCards.Add(item);
            }
        }
    }
    #endregion

    #region 计算当前赢家
    private int GetWinnerSeat()
    {
        // FIXME:当前判断赢家如果两者的牌力值相同有时候返回前者有时候返回后者
        List<int> index = [];
        List<Suit> suits = [];
        List<SelectedHandComposition> selectedHandCompositions = [];
        // 先排除不是同一个花色的
        for (int i = 0; i < playedCards.Length; i++)
        {
            SelectedHandComposition cmp = new([.. CardData.Serialize(playedCards[i])]);
            selectedHandCompositions.Add(cmp);
            if (!RuleEngine.IsSameSuit([.. CardData.Serialize(playedCards[i])], trumpCardData)) continue;
            suits.Add(RuleEngine.GetSuit(playedCards[i][0], trumpCardData));
            index.Add(i);
        }
        if (!index.Contains(dealer))
        {
            GD.PrintErr("庄家选择了不同颜色的牌");
            return -1;
        }
        int dealerIndex = index.IndexOf(dealer);
        SelectedHandComposition dealerCmp = selectedHandCompositions[dealerIndex];
        if (index.Count == 1)
            return index[0];
        if (turnData.playType == PlayType.THROW_CARD)
        {
            List<int> killIndex = [];
            for (int i = 0; i < suits.Count; i++)
            {
                if (suits[i] == Suit.NONE)
                    killIndex.Add(index[i]);
            }
            if (killIndex.Count == 0 || suits[dealerIndex] == Suit.NONE) return dealer;
            // 有人杀牌
            bool dealerHasTractor = dealerCmp.Tractors.Count > 0;
            bool dealerHasDouble = dealerCmp.Doubles.Count > 0;
            if (dealerHasTractor) // 连对模式
            {
                return GetGreatIndexOfEvenCorrectNone(killIndex, selectedHandCompositions);
            }
            else if (dealerHasDouble) // 对子模式
            {
                return GetGreatIndexOfDoubleNone(killIndex, selectedHandCompositions);
            }
            else
            {
                // 单牌模式，看最大的牌就可以了
                return GetGreatIndexOfSingleNone(killIndex, selectedHandCompositions);
            }
        }
        else if (turnData.playType == PlayType.SINGLE)
        {
            return GetGreatIndexOfSingle(index, suits, selectedHandCompositions);
        }
        else if (turnData.playType == PlayType.DOUBLE)
        {
            return GetGreatIndexOfDouble(index, suits, selectedHandCompositions);
        }
        else if (turnData.playType == PlayType.EVEN_CORRECT)
        {
            return GetGreatIndexOfEvenCorrect(index, suits, selectedHandCompositions);
        }
        else
        {
            GD.PrintErr("本轮庄家出牌类型为PlayType.NONE");
            return dealer;
        }
    }

    private int GetGreatIndexOfSingle(List<int> index, List<Suit> suits, List<SelectedHandComposition> selectedHandCompositions)
    {
        int[] values = [-1, -1, -1, -1];
        for (int i = 0; i < index.Count; i++)
        {
            if (selectedHandCompositions[index[i]].Singles.Count != 1)
            {
                GD.PrintErr("单牌模式，有人选择了多张牌");
                return dealer;
            }
            CardData cardData = CardData.Deserialize(selectedHandCompositions[index[i]].Singles[0].Card);
            int value = RuleEngine.GetCardValue(cardData);
            if (suits[i] == Suit.NONE)
            {
                value *= 100;
            }
            values[index[i]] = value;
        }
        return GetMaxIndex(values, "单牌比较的时候没有最大值");
    }
    private int GetGreatIndexOfDouble(List<int> index, List<Suit> suits, List<SelectedHandComposition> selectedHandCompositions)
    {
        int[] values = [-1, -1, -1, -1];
        for (int i = 0; i < index.Count; i++)
        {
            if (selectedHandCompositions[index[i]].Doubles.Count == 0)
            {
                GD.Print($"双牌模式，{index[i]}没有双牌");
                continue;
            }
            if (selectedHandCompositions[index[i]].Doubles.Count != 1)
            {
                GD.PrintErr("双牌模式，有人选择了多张牌");
                return dealer;
            }
            CardData cardData = CardData.Deserialize(selectedHandCompositions[index[i]].Doubles[0].Card1);
            int value = RuleEngine.GetCardValue(cardData);
            if (suits[i] == Suit.NONE)
            {
                value *= 100;
            }
            values[index[i]] = value;
        }
        return GetMaxIndex(values, "双牌比较的时候没有最大值");
    }
    private int GetGreatIndexOfEvenCorrect(List<int> index, List<Suit> suits, List<SelectedHandComposition> selectedHandCompositions)
    {
        int[] values = [-1, -1, -1, -1];
        for (int i = 0; i < index.Count; i++)
        {
            if (selectedHandCompositions[index[i]].Tractors.Count == 0)
            {
                GD.Print($"连对牌模式，{index[i]}没有连对");
                continue;
            }
            if (selectedHandCompositions[index[i]].Tractors.Count != 1)
            {
                GD.PrintErr("连对模式，有人选择了连对牌");
                return dealer;
            }
            int value = selectedHandCompositions[index[i]].Tractors[0].BiggestValue();
            if (suits[i] == Suit.NONE)
            {
                value *= 100;
            }
            values[index[i]] = value;
        }
        return GetMaxIndex(values, "连对比较的时候没有最大值");
    }

    private int GetGreatIndexOfEvenCorrectNone(List<int> killIndex, List<SelectedHandComposition> selectedHandCompositions)
    {
        int[] values = [-1, -1, -1, -1];
        values[dealer] = 0;
        Tractor dealerTractor = selectedHandCompositions[dealer].GetLargestTractor();
        if (dealerTractor == null)
        {
            GD.PrintErr("获取最大连对时候出现问题");
            return dealer;
        }
        for (int i = 0; i < killIndex.Count; i++)
        {
            Tractor tractor = selectedHandCompositions[killIndex[i]].GetLargestTractor();
            if (tractor == null)
            {
                GD.Print($"获取最大连对时候出现问题，{killIndex[i]}没有连对");
                continue;
            }
            if (tractor.GetCount() >= dealerTractor.GetCount())  // 只要当前的长度大于庄家出牌的长度就好了
            {
                values[killIndex[i]] = tractor.GetCount() * 100 + tractor.BiggestValue();
            }
        }
        return GetMaxIndex(values, "连对比较的时候没有最大值");
    }

    private int GetGreatIndexOfDoubleNone(List<int> killIndex, List<SelectedHandComposition> selectedHandCompositions)
    {
        int[] values = [-1, -1, -1, -1];
        values[dealer] = 0;
        for (int i = 0; i < killIndex.Count; i++)
        {
            DoubleCard doubleCard = selectedHandCompositions[killIndex[i]].GetLargestDouble();
            if (doubleCard == null)
            {
                GD.Print($"获取最大对时候出现问题，{killIndex}没有对");
                continue;
            }
            values[killIndex[i]] = RuleEngine.GetCardValue(CardData.Deserialize(doubleCard.Card1));
        }
        return GetMaxIndex(values, "连对比较的时候没有最大值");
    }

    private int GetGreatIndexOfSingleNone(List<int> killIndex, List<SelectedHandComposition> selectedHandCompositions)
    {
        int[] values = [-1, -1, -1, -1];
        values[dealer] = 0;
        for (int i = 0; i < killIndex.Count; i++)
        {
            int value = selectedHandCompositions[killIndex[i]].GetLargestSingleCardValue();
            if (value == -1)
            {
                GD.Print($"获取最大单张时候出现问题，{killIndex}没有任何牌");
            }
            values[killIndex[i]] = value;
        }
        return GetMaxIndex(values, "连对比较的时候没有最大值");
    }
    private int GetMaxIndex(int[] values, string info)
    {
        int maxValue = -1;
        int maxIndex = -1;
        for (int i = 0; i < values.Length; i++)
        {
            int index = (i + dealer) % GameSettings.PLAYER_COUNT;
            if (values[index] > maxValue)
            {
                maxValue = values[index];
                maxIndex = index;
            }
        }
        if (maxIndex == -1)
        {
            GD.PrintErr(info);
            return dealer;
        }
        return maxIndex;
    }

    #endregion
}