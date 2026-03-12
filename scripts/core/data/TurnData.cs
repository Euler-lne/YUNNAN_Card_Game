public class TurnData
{
    public PlayType playType;
    public int playNum;
    public Suit suit;

    public TurnData(PlayType playType, int playNum, Suit suit)
    {
        this.playType = playType;
        this.playNum = playNum;
        this.suit = suit;
    }

    public TurnData()
    {
        playNum = 0;
        playType = PlayType.NONE;
        suit = Suit.NONE;
    }

    public static int Serialize(TurnData turnData)
    {
        // 分配位数：playNum 需要6位（最多63），playType 和 suit 各需3位（最多7）
        // 布局：高6位 - playNum，中间3位 - playType，低3位 - suit
        return (turnData.playNum << 6) | ((int)turnData.playType << 3) | (int)turnData.suit;
    }

    public static TurnData Deserialize(int id)
    {
        int suit = id & 0x7;               // 取低3位
        int playType = (id >> 3) & 0x7;    // 取中间3位
        int playNum = id >> 6;              // 取高6位
        return new TurnData((PlayType)playType, playNum, (Suit)suit);
    }
}