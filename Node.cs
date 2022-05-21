namespace SymboMath;
using System.Text.RegularExpressions;
using System.Linq;

public abstract class Node
{
    public static Node ParsePostfix( string[] Postfix )
    {
        if ( int.TryParse( Postfix[ ^1 ], out int IntNode ) )
            return new Node<int>( IntNode );
        if ( float.TryParse( Postfix[ ^1 ], out float FloatNode ) )
            return new Node<float>( FloatNode );
        if ( IsUnaryOperator( Postfix[ ^1 ], out Operator uo ) )
            return new UnaryNode( uo, ParsePostfix( Postfix[ ..^1 ] ) );
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
            return new PlenaryNode( po, ParsePostfix( arg1 ), ParsePostfix( arg2 ) );
        }
        if ( Postfix[ ^1 ].Length == 1 )
            return new Node<char>( Postfix[ ^1 ][ 0 ] );
        return new Node<string>( Postfix[ ^1 ] );
    }
    public static Node ParseInfix( string Infix )
    {
        return ParsePostfix( InfixToPostfix( Infix ).ToArray() );
    }
}
public class Node<T> : Node
{
    public Node( T Value )
    {
        this.Value = Value;
    }
    public T Value;
    public static bool operator ==( Node<T> a, Node<T> b ) => a.ToString() == b.ToString();
    public static bool operator !=( Node<T> a, Node<T> b ) => a.ToString() != b.ToString();
    public override bool Equals( object? obj ) => obj is Node n && ToString() == n.ToString();
    public override int GetHashCode() => ( ToString() ?? "" ).GetHashCode();
    public override string? ToString() => ( Value ?? throw new ArgumentNullException( "Value of node is null" ) ).ToString();
    public void Simplify()
    {

    }
}

public abstract class OpNode : Node<Operator>
{
    protected OpNode( Operator val, params Node[] links ) :
        base( val )
    {
        this.links = links;
    }
    protected Node[] links;
    public abstract int LinkCount { get; }
    public abstract Node this[ int n ] { get; set; }
}
public class UnaryNode : OpNode
{
    public UnaryNode( Operator Value, Node link ) :
        base( Value, link )
    {
    }
    public override int LinkCount { get => 1; }
    public override Node this[ int n ]
    {
        get => links[ n == 0 ? 0 : throw new ArgumentOutOfRangeException( $"Unary node only has one link - cannot get link number {n}" ) ];
        set => links[ n == 0 ? 0 : throw new ArgumentOutOfRangeException( $"Unary node only has one link - cannot set link number {n}" ) ] = value ?? throw new ArgumentNullException( nameof( value ) );
    }
    public override string? ToString() 
    {
        string s = OperatorValue( Value ) + " ( " + links[ 0 ].ToString() + " )";
        while ( Regex.Match( s, "  " ).Success )
            s = Regex.Replace( s, "  ", " " );
        return s;
    }
}
public class PlenaryNode : OpNode
{
    public PlenaryNode( Operator Value, params Node[] links ) :
        base( Value, links )
    {
    }
    public override int LinkCount { get => links.Length; }
    public override Node this[ int n ]
    {
        get => links[ n ];
        set => links[ n ] = value ?? throw new ArgumentNullException( nameof( value ) );
    }
    public override string? ToString()
    {
        string s = "";
        for ( int i = 0; i < links.Length; ++i )
            s += " ( " + links[ i ] + " ) " + ( i + 1 != links.Length ? OperatorValue( Value ) : "" );
        while ( Regex.Match( s, "  " ).Success )
            s = Regex.Replace( s, "  ", " " );
        return s;
    }
    public void BinaryOpsToPlenaryOps()
    {
        for ( int i = 0; i < links.Length; ++i )
        {
            if ( links[ i ] is PlenaryNode no && Value == no.Value )
            {
                links = links[ ..i ].Concat( no.links ).Concat( links[ ( i + 1 ).. ] ).ToArray();
                --i;
            }
        }
        foreach ( Node link in links )
        {
            if ( link is PlenaryNode PlenaryLink )
                PlenaryLink.BinaryOpsToPlenaryOps();
        }
    }
}