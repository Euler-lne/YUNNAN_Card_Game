using System.Collections.Generic;
using Godot;
using Euler.Global;

public class GameData
{
    public int DealerSeat { get; set; }

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
        Rank.QUEEN,
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