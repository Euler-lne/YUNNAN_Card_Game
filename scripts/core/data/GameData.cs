using System.Collections.Generic;
using Godot;
using Euler.Global;

public class GameData
{
    public int DealerSeat { get; set; }          // 庄家座位，-1 表示未定
    public int OpponentScore { get; set; }        // 闲家累计得分

    public bool IsSnatchDealer { get; set; }

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
        IsSnatchDealer = true;
        RoundIndex = 1;

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

    public void WinRound(int seat, int step = 1)
    {
        DealerSeat = seat;
        Rank current = GetCurrentRank()[GetTeamIndex(DealerSeat)];

        int index = System.Array.IndexOf(LevelOrder, current);

        if (index == -1)
        {
            GD.PrintErr("等级不在升级序列中！");
            return;
        }

        int nextIndex = (index + step) % LevelOrder.Length;

        TeamLevels[DealerSeat] = LevelOrder[nextIndex];
    }

    public void DrawRound()
    {
        IsSnatchDealer = true;
    }
}