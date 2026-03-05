using System.Numerics;

namespace Euler.Global
{
    public static class CardParams
    {
        public const float CARD_SCALE = 0.4f;
        public const float CARD_HEIGHT = 222 * 2 * CARD_SCALE;
        public const float CARD_WIDTH = 156 * 2 * CARD_SCALE;
        public const float CARD_PUT_SCALE = 0.75f;
        public const string CARD_BACK_PATH = "res://assets/back_black_design_04.png";
    }

    public static class CardLayoutParams
    {
        // 固定左右边距（像素）
        public const float HORIZONTAL_MARGIN = 50f;
        public const float BOTTOM_MARGIN_UNSELECT = CardParams.CARD_HEIGHT * 0.75f;
        public const float BOTTOM_MARGIN_MOVE = CardParams.CARD_HEIGHT - BOTTOM_MARGIN_UNSELECT;
        public const float PUT_CARD_MARGIN = 5f;
        public const float PUT_CARD_SPACING = 20f;
    }

    public static class GameSettings
    {
        public const int PLAYER_COUNT = 4;
        public const int CARD_PRE_PLAYER = 25;
        public const float DEAL_DURATION_TIME = 0.4f;
    }
}

