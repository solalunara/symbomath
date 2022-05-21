namespace SymboMath;

public enum Operator
{
    NONE    = 0,
    SBT     = 1 << 0,
    DIV     = 1 << 1,
    EXP     = 1 << 2,
    LN      = 1 << 3,
    UnaryOperator = 1 << 0 | 1 << 1 | 1 << 2 | 1 << 3,
    ADD     = 1 << 4,
    MULT    = 1 << 5,
    PlenaryOperator = 1 << 4 | 1 << 5
}