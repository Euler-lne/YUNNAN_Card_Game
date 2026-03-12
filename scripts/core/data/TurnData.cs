public class TurnData
{
    public PlayType playType;
    public int playNum;

    public TurnData(PlayType playType, int playNum)
    {
        this.playType = playType;
        this.playNum = playNum;
    }

    public TurnData()
    {
        playNum = 0;
        playType = PlayType.NONE;
    }

    public static int Serialize(TurnData turnData)
    {
        return turnData.playNum * 10 + (int)turnData.playType;
    }

    public static TurnData Deserialize(int id)
    {
        int playNum = id % 10;
        id /= 10;
        return new((PlayType)id, playNum);
    }
}