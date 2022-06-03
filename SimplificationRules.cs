namespace SymboMath;
public enum Preference { Addition, Multiplication }
public enum Mode { Condense = 0, Expand = 1 }
public struct SimplificationRules
{
    public bool DistributeNegative;
    public bool FactorNegative { get => !DistributeNegative; }
    public bool DistributeDivision;
    public bool FactorDivision { get => !DistributeDivision; }
    public bool DistributeAddition;
    public bool FactorAddition { get => !DistributeAddition; }
    public bool PreferRepeatMult;
    public bool PreferExpToMult { get => !PreferRepeatMult; }
    public Preference Prefer;
    public Mode Exp;
    public Mode Ln;
}