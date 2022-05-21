[Flags]
public enum SimplificationRules : uint
{
    Default                = 0,
    Move_Negatives_Up   = 1 << 0,
    Distribute_Times    = 1 << 1,
    Isolate_Division    = 1 << 2,
    Condense_Fractions  = 1 << 3,
    Prefer_Exp_To_Mult  = 1 << 4,
    Prefer_Positive_Exp = 1 << 5,
    Prefer_Positive_Ln  = 1 << 6,
    Distribute_Exponents= 1 << 7,
    Condense_Ln         = 1 << 8,
    Condense_Exp        = 1 << 9,
    All                 = 0xffffffff
}