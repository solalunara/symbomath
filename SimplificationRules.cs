namespace SymboMath;
[Flags]
public enum SimplificationRules : uint
{
    Default             = 0,
    DistributeNegative  = 1 << 0,
    DistributeDivision  = 1 << 1,
    DistributeAddition  = 1 << 2,
    PreferAddition      = 1 << 4,
    PreferMultiplication= 1 << 5,
    PreferRepeatMult    = 1 << 6,
    ExpandLnMultToAdd   = 1 << 7,
    CondenseLnAddToMult = 1 << 8,
    ExpandLnDivToNeg    = 1 << 9,
    CondenseLnNegToDiv  = 1 << 10,
    ExpandExpAddToMult  = 1 << 11,
    CondenseExpMultToAdd= 1 << 12,
    ExpandExpNegToDiv   = 1 << 13,
    CondenseExpDivToNeg = 1 << 14,
    All                 = 0xffffffff
}