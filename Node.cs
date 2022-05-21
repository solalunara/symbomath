namespace SymboMath;
using System.Text.RegularExpressions;
using System.Linq;

public abstract class Node
{
    protected Node( Node[] links )
    {
        this.links = links;
    }
    private Node[] links;
    public Node this[ int n ]
    {
        get => links[ n ];
        set => links[ n ] = value;
    }
    public static Node ParsePostfix( string[] Postfix )
    {
        if ( int.TryParse( Postfix[ ^1 ], out int IntNode ) )
            return new Node<int>( IntNode );
        if ( float.TryParse( Postfix[ ^1 ], out float FloatNode ) )
            return new Node<float>( FloatNode );
        if ( IsUnaryOperator( Postfix[ ^1 ], out Operator uo ) )
            return new Node<Operator>( uo, ParsePostfix( Postfix[ ..^1 ] ) );
        if ( IsPlenaryOperator( Postfix[ ^1 ], out Operator po ) )
        {
            int n = Postfix.Length - 2;
            for ( int Count = 1; Count != 0; )
            {
                if ( IsUnaryOperator( Postfix[ n ], out _ ) )
                    Count += 1;
                if ( IsPlenaryOperator( Postfix[ n ], out _ ) )
                    Count += 2;
                --n;
                --Count; //needs to be modified *before* the check
            }
            int arg1n = n + 1; //we want where arg1 begins, not where arg2 ends
            string[] arg1 = Postfix[ arg1n..^1 ];
            for ( int Count = 1; Count != 0; )
            {
                if ( IsUnaryOperator( Postfix[ n ], out _ ) )
                    Count += 1;
                if ( IsPlenaryOperator( Postfix[ n ], out _ ) )
                    Count += 2;
                --n;
                --Count; //needs to be modified *before* the check
            }
            ++n;
            string[] arg2 = Postfix[ n..arg1n ];
            return new Node<Operator>( po, ParsePostfix( arg1 ), ParsePostfix( arg2 ) );
        }
        if ( Postfix[ ^1 ].Length == 1 )
            return new Node<char>( Postfix[ ^1 ][ 0 ] );
        return new Node<string>( Postfix[ ^1 ] );
    }
    public static Node ParseInfix( string Infix )
    {
        return ParsePostfix( InfixToPostfix( Infix ).ToArray() );
    }
    public void BinaryOpsToPlenaryOps()
    {
        if ( this is Node<Operator> o && IsPlenaryOperator( o ) )
        {
            for ( int i = 0; i < links.Length; ++i )
            {
                if ( links[ i ] is Node<Operator> no && o.Value == no.Value )
                {
                    o.links = o.links[ ..i ].Concat( no.links ).Concat( o.links[ ( i + 1 ).. ] ).ToArray();
                    --i;
                }
            }
        }
        foreach ( Node link in links )
        {
            link.BinaryOpsToPlenaryOps();
        }
    }
}

public class Node<T> : Node
{
    public Node( T Value, params Node[] links ) : 
        base( links )
    {
        this.Value = Value;
    }
    public T Value;
    public static implicit operator T( Node<T> n ) => n.Value;
}