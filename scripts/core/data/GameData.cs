using System.Collections.Generic;
using Godot;
using Euler.Global;

public class GameData
{
    // TODO:两种情况，平手那么需要强庄，这时候各队强各队的，谁先强到谁是庄
    public int DealerSeat { get; set; }          // 庄家座位，-1 表示未定
    public int CurrentTurnSeat { get; set; }      // 当前出牌玩家
    public int OpponentScore { get; set; }        // 闲家累计得分

    public Dictionary<int, Rank> TeamLevels { get; private set; }

    public int RoundIndex { get; set; }

    public GamePhase CurrentPhase { get; set; }

    public TrumpState TrumpState { get; set; }

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
        RoundIndex = 1;

        TeamLevels = new Dictionary<int, Rank>
        {
            // 两个队
            [0] = Rank.TWO,
            [1] = Rank.TWO
        };
    }

    private static int GetTeamIndex(int seat)
    {
        return seat % (GameSettings.PLAYER_COUNT / 2); // 0&2 一队, 1&3 一队
    }

    public Rank GetCurrentRank()
    {
        // FIXME：建议返回一个LIST用于判断强庄的情况
        int team = GetTeamIndex(DealerSeat);
        return TeamLevels[team];
    }

    public void WinRound(int seat, int step = 1)
    {
        DealerSeat = seat;
        Rank current = GetCurrentRank();

        int index = System.Array.IndexOf(LevelOrder, current);

        if (index == -1)
        {
            GD.PrintErr("等级不在升级序列中！");
            return;
        }

        int nextIndex = (index + step) % LevelOrder.Length;

        TeamLevels[DealerSeat] = LevelOrder[nextIndex];
    }
}