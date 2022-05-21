global using static SymboMath.Statics;
using SymboMath;

Node n1 = Node.ParseInfix( "3 * 4 * 5 * ( x + y )" );
Node n2 = Node.ParseInfix( "3 * ( x + y ) * 4 * 5" );
Console.WriteLine( n1.ToString() );
Console.WriteLine( n2.ToString() );
Console.WriteLine( n1 == n2 );