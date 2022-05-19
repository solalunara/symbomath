namespace SymboMath;

public enum UnaryOperator
{
    NONE    = 0,
    SBT     = 1 << 0,
    DIV     = 1 << 1,
    EXP     = 1 << 2,
    LN      = 1 << 3
}
public enum PlenaryOperator
{
    NONE    = 0,
    ADD     = 1 << 0,
    MULT    = 1 << 1,
}