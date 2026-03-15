using System.Collections.Generic;
using Godot;
using Euler.Global;
using Euler.Event;
using System;

public class GameData
{
    public int DealerSeat { get; set; }          // 庄家座位
    private int idlePlayerScore;
    public int IdlePlayerScore
    {
        get { return idlePlayerScore; }
        set
        {
            idlePlayerScore = value;
            EventBus.OnChangeIdlePlayerScoreEvent(value);
        }
    }        // 闲家累计得分

    public bool IsSnatchDealer { get; set; }

    public Dictionary<int, Rank> TeamLevels { get; private set; }
    public GamePhase CurrentPhase { get; set; }

    public TrumpState TrumpState { get; set; }

    public int WinnerSeat { get; set; }

    private static readonly Rank[] LevelOrder =
    [
        Rank.TWO,
        Rank.FIVE,
        Rank.TEN,
        Rank.JACK,
        Rank.KING,
        Rank.ACE
    ];

    public GameData()
    {
        TrumpState = new TrumpState();
        DealerSeat = 0;
        IsSnatchDealer = true;
        WinnerSeat = -1;

        TeamLevels = new Dictionary<int, Rank>
        {
            // 两个队
            [0] = Rank.TWO,
            [1] = Rank.TWO
        };
    }

    public static int GetTeamIndex(int seat)
    {
        return seat % (GameSettings.PLAYER_COUNT / 2); // 0&2 一队, 1&3 一队
    }

    public List<Rank> GetCurrentRank()
    {
        List<Rank> ranks = [];
        ranks.Add(TeamLevels[0]);
        ranks.Add(TeamLevels[1]);
        return ranks;
    }

    public void WinRound(int winnerSeat, ScoreResult scoreResult, bool isWinnerDealer)
    {
        switch (scoreResult)
        {
            case ScoreResult.DealerUp3:
                if (isWinnerDealer)
                {
                    DealerSeat = winnerSeat;
                    IncreaseLeve(winnerSeat, 3);
                }
                else
                {
                    IncreaseLeve(DealerSeat, 3);
                }
                break;
            case ScoreResult.DealerUp2:
                if (isWinnerDealer)
                {
                    DealerSeat = winnerSeat;
                    IncreaseLeve(winnerSeat, 2);
                }
                else
                {
                    IncreaseLeve(DealerSeat, 2);
                }
                break;
            case ScoreResult.DealerUp1:
                if (isWinnerDealer)
                {
                    DealerSeat = winnerSeat;
                    IncreaseLeve(winnerSeat, 1);
                }
                else
                {
                    IncreaseLeve(DealerSeat, 1);
                }
                break;
            case ScoreResult.DealerStrong:
                DealerSeat = winnerSeat;
                IsSnatchDealer = true;
                break;
            case ScoreResult.DealerDown:
                if (!isWinnerDealer)
                    DealerSeat = winnerSeat;
                break;
            case ScoreResult.IdleUp2:
                if (!isWinnerDealer)
                {
                    DealerSeat = winnerSeat;
                    IncreaseLeve(winnerSeat, 1);
                }
                else
                {
                    IncreaseLeve(DealerSeat, 1);
                }
                break;
            case ScoreResult.IdleUp3:
                if (!isWinnerDealer)
                {
                    DealerSeat = winnerSeat;
                    IncreaseLeve(winnerSeat, 2);
                }
                else
                {
                    IncreaseLeve(DealerSeat, 2);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scoreResult), scoreResult, null);
        }
    }

    public void WinRoundJA(int seat, ScoreResult scoreResult)
    {
        DealerSeat = seat;
        switch (scoreResult)
        {
            case ScoreResult.DealerUp3:
                TeamLevels[GetTeamIndex(DealerSeat)] = Rank.ACE;
                break;
            case ScoreResult.DealerUp2:
                TeamLevels[GetTeamIndex(DealerSeat)] = Rank.ACE;
                break;
            case ScoreResult.DealerUp1:
                TeamLevels[GetTeamIndex(DealerSeat)] = Rank.KING;
                break;
            case ScoreResult.DealerStrong:
                IsSnatchDealer = true;
                // 强庄
                break;
            case ScoreResult.DealerDown:
                // 庄家下场
                break;
            case ScoreResult.IdleUp2:
                // 闲家升1级
                IncreaseLeve(seat, 1);
                break;
            case ScoreResult.IdleUp3:
                IncreaseLeve(seat, 2);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scoreResult), scoreResult, null);
        }
    }
    public void IncreaseLeve(int seat, int step)
    {
        int teamIdx = GetTeamIndex(seat);
        Rank currentRank = TeamLevels[teamIdx];
        int currentIdx = Array.IndexOf(LevelOrder, currentRank);
        if (currentIdx == -1)
        {
            GD.PrintErr("等级不在升级序列中！");
            return;
        }

        int jIdx = Array.IndexOf(LevelOrder, Rank.JACK);
        int aIdx = Array.IndexOf(LevelOrder, Rank.ACE);
        int targetIdx;

        // 根据当前等级所在区间，限制最大可达到的索引
        if (currentIdx <= jIdx)
        {
            // 当前等级小于J，最多只能升到J
            targetIdx = currentIdx + step;
            if (targetIdx > jIdx)
                targetIdx = jIdx;
        }
        else if (currentIdx <= aIdx)
        {
            // 当前等级在J和A之间（含J但不含A），最多只能升到A
            targetIdx = currentIdx + step;
            if (targetIdx > aIdx)
                targetIdx = aIdx;
        }
        else
        {
            // 当前等级已经是A或更大（实际上A是最大），最多升到数组末尾
            targetIdx = currentIdx + step;
            if (targetIdx >= LevelOrder.Length)
                targetIdx = LevelOrder.Length - 1;
        }

        TeamLevels[teamIdx] = LevelOrder[targetIdx];
    }
    public void RollBack(Rank rank, int oldDealer)
    {
        int team = GetTeamIndex(oldDealer);
        TeamLevels[team] = rank == Rank.JACK ? Rank.TWO : Rank.JACK;
    }

}