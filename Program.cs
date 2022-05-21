global using static SymboMath.Statics;
using SymboMath;

Node n = Node.ParseInfix( "x ^ 2 + 3" );
((PlenaryNode)n).BinaryOpsToPlenaryOps();
Console.WriteLine( n.ToString() );