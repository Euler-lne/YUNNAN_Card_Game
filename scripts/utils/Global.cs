using System.Numerics;

namespace Euler.Global
{
    public static class CardParams
    {
        public const float CARD_SCALE = 0.4f;
        public const float CARD_HEIGHT = 222 * 2 * CARD_SCALE;
        public const float CARD_WIDTH = 156 * 2 * CARD_SCALE;
        public const float CARD_PUT_SCALE = CARD_SCALE * 1f;
        public const string CARD_BACK_PATH = "res://assets/back_black_design_04.png";
    }

    public static class CardLayoutParams
    {
        // 固定左右边距（像素）
        public const float HORIZONTAL_MARGIN = 50f;
        public const float BOTTOM_MARGIN_UNSELECT = CardParams.CARD_HEIGHT * 0.75f;
        public const float BOTTOM_MARGIN_MOVE = CardParams.CARD_HEIGHT - BOTTOM_MARGIN_UNSELECT;
        public const float CARD_PUT_DECLARE_LEFT_OFFSET = CardParams.CARD_WIDTH * CardParams.CARD_PUT_SCALE * 1.2f;
        public const float CARD_PUT_DOWN_OFFSET = CardParams.CARD_HEIGHT * CardParams.CARD_PUT_SCALE * 0.2f;
    }

    public static class GameSettings
    {
        public const int PLAYER_COUNT = 4;
        public const int CARD_PRE_PLAYER = 25;
        public const float DEAL_DURATION_TIME = 0.4f;
    }
}

