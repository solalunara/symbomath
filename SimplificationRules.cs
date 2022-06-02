namespace SymboMath;
public enum Preference { Addition, Multiplication }
public enum ExpressionMode { Condense = 0, Expand = 1 }
public enum FunctionMode { Factor = 0, Distribute = 1 }
public struct SimplificationRules
{
    public FunctionMode Negative;
    public FunctionMode Division;
    public FunctionMode Addition;
    public Preference Prefer;
    public ExpressionMode Exp;
    public ExpressionMode Ln;
}