namespace Euler.Global
{
    public static class CardParams
    {
        public const float CARD_SCALE = 0.7f;
        public const float CARD_HEIGHT = 222 * 2 * CARD_SCALE;
        public const float CARD_WIDTH = 156 * 2 * CARD_SCALE;
    }

    public static class CardLayoutParams
    {
        // 固定左右边距（像素）
        public const float HORIZONTAL_MARGIN = 50f;
        public const float BOTTOM_MARGIN_UNSELECT = CardParams.CARD_HEIGHT * 0.75f;
        public const float BOTTOM_MARGIN_MOVE = CardParams.CARD_HEIGHT - BOTTOM_MARGIN_UNSELECT;

    }
}
