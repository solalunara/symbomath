namespace SymboMath;
public enum Preference { Addition, Multiplication }
public enum Mode { Condense = 0, Expand = 1, Factor = 0, Distribute = 1 }
public struct SimplificationRules
{
    public bool DistributeNegative;
    public bool DistributeDivision;
    public bool DistributeAddition;
    public bool PreferRepeatMult;
    public Preference Prefer;
    public Mode Exp;
    public Mode Ln;
}