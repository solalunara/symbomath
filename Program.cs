global using static SymboMath.Statics;
using SymboMath;

Node n = Node.ParseInfix( "- ( 2 * x * y + 4 )" );
n.BinaryOpsToPlenaryOps();
Console.WriteLine( n.ToString() );