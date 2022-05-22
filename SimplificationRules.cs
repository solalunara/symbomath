[Flags]
public enum SimplificationRules : uint
{
    Default             = 0,
    Distribute          = 1 << 0,
    Condense_Fractions  = 1 << 1,
    Prefer_Exp_To_Mult  = 1 << 2,
    Expand_Ln           = 1 << 3,
    Expand_Exp          = 1 << 4,
    All                 = 0xffffffff
}