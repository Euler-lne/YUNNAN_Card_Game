using Godot;

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

    public void Print()
    {
        GD.Print("-------打印当前叫主状态--------");
        GD.Print($"当前庄家{dealerSeat}");
        GD.Print($"当前主花色{trumpSuit}");
        GD.Print($"当前是否有主{haveTrump}");
        GD.Print($"当前是否锁主{isLocked}");
        GD.Print("-------打印结束--------");
    }
}