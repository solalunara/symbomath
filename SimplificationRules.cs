namespace SymboMath;
public enum Preference { Addition, Multiplication }
public enum Mode { Condense, Expand }
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