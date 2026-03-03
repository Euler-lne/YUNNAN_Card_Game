public class TrumpState
{
    public int dealerSeat = -1;      // 庄家是谁
    public TrumpSuit trumpSuit = TrumpSuit.UNKNOW_TRUMP; // 主花色
    public bool haveTrump = false;   // 是否无主
    public bool isLocked = false;    // 是否定主（对子锁主）

    public static TrumpSuit ToTrumpSuit(Suit suit)
    {
        return suit switch
        {
            Suit.SPADE => TrumpSuit.SPADE,
            Suit.HEART => TrumpSuit.HEART,
            Suit.CLUB => TrumpSuit.CLUB,
            Suit.DIAMOND => TrumpSuit.DIAMOND,
            Suit.NONE => TrumpSuit.NONE_TRUMP,
            _ => TrumpSuit.UNKNOW_TRUMP
        };
    }
}