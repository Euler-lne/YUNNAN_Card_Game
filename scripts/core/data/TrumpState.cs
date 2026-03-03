public class TrumpState
{
    public int dealerSeat = -1;      // 庄家是谁
    public TrumpSuit trumpSuit = TrumpSuit.UNKNOW_TRUMP; // 主花色
    public bool isNoTrump = false;   // 是否无主
    public bool isLocked = false;    // 是否定主（对子锁主）
}