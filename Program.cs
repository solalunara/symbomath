global using static SymboMath.Statics;
using SymboMath;

Node n = Node.ParseInfix( "- ( 2 * x * y + 4 )" );
n.BinaryOpsToPlenaryOps();
Console.Write( "you should be in the debugger by now" );