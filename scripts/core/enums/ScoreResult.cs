public enum ScoreResult
{
    DealerUp3 = 0,      // 庄家升3级 (0分)
    DealerUp2 = 1,      // 庄家升2级 (0 < score < 40)
    DealerUp1 = 2,      // 庄家升1级 (40 ≤ score < 80)
    DealerStrong = 3,   // 强庄 (score == 80)
    DealerDown = 4,     // 庄家下场 (80 < score < 120)
    IdleUp2 = 5,        // 闲家升2级 (120 ≤ score < 160)
    IdleUp3 = 6         // 闲家升3级 (160 ≤ score < 200)
}