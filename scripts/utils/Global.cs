using System;
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
        public const float DEAL_DURATION_TIME = 0.25f;

        public const float DEAL_END_TIME = 1f;
        public const float INFO_EXIST_TIME = 1f;
        public static string[] GetRandomFourNames()
        {
            // 复制一份数组，避免修改原数组
            string[] shuffled = (string[])DEFAULT_NAME_LIST.Clone();

            // Fisher-Yates 洗牌算法
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            // 取前4个
            return [shuffled[0], shuffled[1], shuffled[2], shuffled[3]];
        }

        public static string[] GetAvatarList()
        {
            return [AVATAR_JOKER, AVATAR_JACK, AVATAR_QUEEN, AVATAR_KING];
        }

        private const string AVATAR_KING = "res://assets/human_king_of_diamond_red_design_2.png";
        private const string AVATAR_JACK = "res://assets/human_jack_of_diamond_red_design_2.png";
        private const string AVATAR_QUEEN = "res://assets/human_queen_of_diamond_red_design_2.png";
        private const string AVATAR_JOKER = "res://assets/joker_big.png";

        private static readonly string[] DEFAULT_NAME_LIST =
        [
            // 1字名（古风）
            "墨", "尘", "弦", "砚", "岚",
            // 1字名（科幻）
            "零", "蚀", "烬", "梭", "光",

            // 2字名（古风）
            "青鸾", "鹤影", "松烟", "竹喧", "鹿鸣",
            "拾光", "听雨", "问雪", "煮酒", "焚香",
            
            // 2字名（科幻）
            "脉冲", "星环", "以太", "奇点", "流明",
            "零式", "暗核", "回声", "赛琳", "天枢",
            
            // 3字名（古风）
            "云中鹤", "花间酒", "月下客", "醉清风", "踏雪行",
            "南柯梦", "北冥鱼", "东篱菊", "西江月", "山外山",
            
            // 3字名（科幻）
            "星之痕", "光速旅", "深空眼", "量子猫", "暗物质",
            "赛博熊", "机械心", "数据海", "银河渡", "零号机",
            
            // 4字名（古风）
            "明月清风", "白云苍狗", "煮鹤焚琴", "踏雪寻梅", "烟雨江南",
            "长安落雪", "孤舟蓑笠", "古道西风", "小桥流水", "对酒当歌",
            
            // 4字名（科幻）
            "暗夜流光", "星尘远征", "量子纠缠", "时间裂缝", "机械纪元",
            "赛博空间", "银河铁道", "未来回声", "深空信号", "零号基地"
        ];
        private static readonly Random random = new();

    }
}

