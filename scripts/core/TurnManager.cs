using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Euler.Event;
using Euler.Global;
using Godot;
public class TurnManager
{
    private TurnRequest turnRequest;
    private int dealer;  // 每一局的dealer
    private int currentSeat;
    private readonly List<CardData>[] playedCards = new List<CardData>[4];
    private TaskCompletionSource playTcs = null;
    private TurnData turnData;
    private CardData trumpCardData;
    private GameCore gameCore;
    public void Init(TurnRequest turnRequest, int dealer, GameCore gameCore)
    {
        this.turnRequest = turnRequest;
        this.dealer = dealer;
        TurnEvent.PlayCardButtonPressEvent = OnPlayCardButtonPress;
        this.gameCore = gameCore;
        turnData = new();
    }


    public void SetFirstTurn()
    {
        trumpCardData = new(gameCore.GetTrumpSuit(), gameCore.GetCurrentRank(gameCore.GetDealerSeat())); ;
    }
    public async void StartTurn(int dealer = -1)
    {
        // 通知dealer进行选择
        if (dealer != -1) this.dealer = dealer;
        currentSeat = this.dealer;
        turnData = new();
        await WaitPlayerPlayCard();
        IncreaseCurrentSeat();
    }
    private void IncreaseCurrentSeat()
    {
        currentSeat++;
        currentSeat %= GameSettings.PLAYER_COUNT;
        if (currentSeat == dealer)
        {
            TurnEnd();
        }
        else
            NextOne();
    }
    private async void NextOne()
    {
        await WaitPlayerPlayCard();
        IncreaseCurrentSeat();
    }
    private void TurnEnd()
    {
        // TODO:判断当前出的牌谁最大，dealer设置为最大的
        // TODO:清空卡牌
        // TODO:如果还有牌那么就继续
        StartTurn();
    }

    private async Task WaitPlayerPlayCard()
    {
        // TODO:等待玩家出牌
        // 1. 通知对应玩家UI显示出牌按钮
        // 2. 等待点击确认按钮，客户端自己简单判断，到服务器再判断一遍
        turnRequest.TurnStart(currentSeat, turnData, gameCore.IsDealer(currentSeat));
        playTcs = new();
        await playTcs?.Task;
        playTcs = null;
    }

    private void OnPlayCardButtonPress(List<CardData> cardDatas)
    {
        if (cardDatas.Count == 0)
        {
            GD.Print("当前选择的牌数为0");
            return;
        }
        List<int> selectCards = [.. CardData.Serialize(cardDatas)];
        PlayType playType = RuleEngine.DetermineSelectedPlayType(selectCards);
        GD.Print($"当前选择的牌的类型{playType}");
        bool isValid = false;
        if (currentSeat == dealer)
        {
            isValid = true;
            if (playType == PlayType.THROW_CARD) // 只有庄家可以出甩牌
            {
                isValid = JudgeThrowCard(cardDatas);
                if (!isValid)
                {
                    // TODO:扣分
                    int increase = gameCore.IsDealer(currentSeat) ? 5 : -5;
                    gameCore.InscreaseIdlePlayerScore(increase);
                }
            }
            if (isValid)
            {
                NetworkManager.Instance.PlayCard(currentSeat, cardDatas, false, gameCore.GetCurrentGamePhase());
                playedCards[currentSeat] = cardDatas;
            }
        }
        else
        {

        }
        GD.Print($"当前是否合法{isValid}");
        if (isValid)
        {
            if (currentSeat == dealer)
            {
                Suit suit = RuleEngine.GetSuit(cardDatas[0], trumpCardData);
                turnData = new(playType, suit, playedCards[currentSeat]);
            }
            turnRequest.TurnEnd(currentSeat);
            playTcs?.SetResult();
            playTcs = null;
        }
        else
        {
            turnRequest.TurnStart(currentSeat, turnData, gameCore.IsDealer(currentSeat)); // 重来
            GD.Print("不合理，重新开始一轮");
        }


        // TODO:更新TurnData，如果是刚开始的时候
    }

    private bool JudgeThrowCard(List<CardData> cardDatas)
    {
        Suit suit = RuleEngine.GetSuit(cardDatas[0], trumpCardData);
        return gameCore.IsBiggest(cardDatas, suit, currentSeat);
    }
}