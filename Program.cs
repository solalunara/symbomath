global using static SymboMath.Statics;
using SymboMath;

SimplificationRules sr = new()
{
    DistributeNegative = true,
    DistributeDivision = true,
    DistributeAddition = true,
    PreferRepeatMult = false,
    Prefer = Preference.Addition,
    Exp = Mode.Condense,
    Ln = Mode.Condense,
};

Node n1 = Node.ParseInfix( "( a * b ) + ( a * - b )" );
Node n2 = n1.Simplify( sr );
Console.WriteLine( n1.ToString() );
Console.WriteLine( n2.ToString() );