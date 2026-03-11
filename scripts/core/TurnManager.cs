using System.Collections.Generic;
using System.Threading.Tasks;
using Euler.Global;
public class TurnManager
{
    private TurnRequest turnRequest;
    private int dealer;  // 每一局的dealer
    private int currentSeat;
    private readonly List<CardData>[] playedCards = new List<CardData>[4];
    private TaskCompletionSource playTcs = null;
    public void Init(TurnRequest turnRequest, int dealer)
    {
        this.turnRequest = turnRequest;
        this.dealer = dealer;
    }

    public async void StartTurn()
    {
        // 通知dealer进行选择
        currentSeat = dealer;
        await WaitPlayerPlayCard();
        IncreaseCurrentSeat();
    }
    private void IncreaseCurrentSeat()
    {
        turnRequest.SetPlayUI(currentSeat, false);
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
        turnRequest.SetPlayUI(currentSeat, true);
        playTcs = new();
        await playTcs?.Task;
        playTcs = null;
    }

    private void OnPlayCardButtonPress()
    {
        // TODO:判断是否合理，函数可以改
        // playTcs?.SetResult();
        // playTcs = null;
    }
}