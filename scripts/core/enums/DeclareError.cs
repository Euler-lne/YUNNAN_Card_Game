public enum DeclareError
{
    None,               // 合规
    InvalidCount,       // 牌数不正确
    RankMismatch,       // 点数不匹配当前等级
    SuitMismatch,       // 花色不一致（仅用于反主）
    InvalidOption       // 无效的声明选项
}