global using static SymboMath.Statics;
using SymboMath;

SimplificationRules sr = new()
{
    DistributeNegative = true,
    DistributeDivision = true,
    DistributeAddition = true,
    Prefer = Preference.Addition,
    Exp = Mode.Expand,
    Ln = Mode.Expand,
};

string? s = "";
while ( ( s = Console.ReadLine() ) != null && s.Length > 0 )
{
    Node n1 = Node.ParseInfix( s );
    Node n2 = n1.Simplify( sr );
    Console.WriteLine( n2.ToString() );
}