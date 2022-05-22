namespace SymboMath;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

public abstract class Node : IComparable<Node>
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
            PlenaryNode pn = new PlenaryNode( po, ParsePostfix( arg1 ), ParsePostfix( arg2 ) ); 
            pn.BinaryOpsToPlenaryOps();
            return pn;
        }
        if ( Postfix[ ^1 ].Length == 1 )
            return new Node<char>( Postfix[ ^1 ][ 0 ] );
        return new Node<string>( Postfix[ ^1 ] );
    }
    public static Node ParseInfix( string Infix )
    {
        return ParsePostfix( InfixToPostfix( Infix ).ToArray() );
    }

    public abstract Node Simplify( SimplificationRules sr );
    public abstract bool IsZero();
    public abstract bool IsOne();
    public static bool operator ==( Node a, Node b )
    {
        //ensure that both are simplified in the same manner
        a.Simplify( SimplificationRules.All );
        b.Simplify( SimplificationRules.All );
        return a.Equals( b );
    }
    public static bool operator !=( Node a, Node b ) => !( a == b );
    public abstract override bool Equals( object? obj );
    public override int GetHashCode()
    {
        Node n = this;
        n.Simplify( SimplificationRules.All );
        return ( n.ToString() ?? "" ).GetHashCode();
    }
    public abstract float GetFloatValue();
    public abstract int CompareTo( Node? o );
}

//we overrided GetHashCode() in Node, but we need to continue overriding Equals(), so just disable the warning
#pragma warning disable CS0659 
public class Node<T> : Node
    where T : IConvertible
{
    public Node( T Value )
    {
        this.Value = Value;
    }
    public T Value;
    public override bool Equals( object? obj ) => obj is Node n && ToString() == n.ToString();
    public override string? ToString() => ( Value ?? throw new ArgumentNullException( "Value of node is null" ) ).ToString();
    public override Node Simplify( SimplificationRules sr )
    {
        return this;
    }
    public override bool IsZero() => GetFloatValue() == 0.0f;
    public override bool IsOne() => GetFloatValue() == 1.0f;
    public override float GetFloatValue() => Value.ToSingle( CultureInfo.CurrentCulture );
    public override int CompareTo( Node? o )
    {
        if ( o is null )
            return 1;
        try
        {
            return MathF.Sign( GetFloatValue() - o.GetFloatValue() );
        } 
        //o is an operator
        catch ( InvalidOperationException )
        {
            //we are not an operator. operators have their own CompareTo method
            return 1; //always put operators before numbers or variables
        }
        //o is a variable
        catch ( InvalidCastException )
        {
            //are we a variable?
            if ( Value is char or string )
                return Math.Sign( GetHashCode() - o.GetHashCode() );
            return -1; //always put variables after numbers
        }
    }
}

public abstract class OpNode : Node<Operator>
{
    protected OpNode( Operator val, params Node[] links ) :
        base( val )
    {
        this.links = links.ToList();
    }
    protected List<Node> links;
    public int LinkCount { get => links.Count; }
    public Node this[ int n ] { get => links[ n ]; set => links[ n ] = value; }
    public override bool IsZero() => false;
    public override bool IsOne() => false;
    public override float GetFloatValue() => throw new InvalidOperationException( "Operator does not have float value" );
    public abstract override int CompareTo( Node? o );
    public override Node Simplify(SimplificationRules sr)
    {
        foreach ( Node link in links )
            link.Simplify( sr );
        return base.Simplify( sr );
    }
}
public class UnaryNode : OpNode
{
    public UnaryNode( Operator Value, Node link ) :
        base( Value, link )
    {
    }
    public override string? ToString() 
    {
        string s = OperatorValue( Value ) + " ( " + links[ 0 ].ToString() + " )";
        while ( Regex.Match( s, "  " ).Success )
            s = Regex.Replace( s, "  ", " " );
        return s;
    }
    public override Node Simplify( SimplificationRules sr )
    {
        //throw exception if div by zero
        if ( Value == Operator.DIV && links[ 0 ].IsZero() )
            throw new DivideByZeroException( $"Cannot divide by zero: {this}" );

        // - - a = / / a = a
        if ( links[ 0 ] is UnaryNode un && un.Value == Value && ( Value is Operator.SBT or Operator.DIV ) )
            return un.links[ 0 ].Simplify( sr );

        // exp( ln( a ) ) = a
        if ( Value == Operator.EXP && links[ 0 ] is UnaryNode unexp && unexp.Value == Operator.LN )
        {
            if ( unexp.Value == Operator.LN )
                return unexp[ 0 ].Simplify( sr );
            else if ( unexp[ 0 ].IsZero() )
                return new Node<int>( 1 );
            else if ( unexp[ 0 ].IsOne() )
                return new Node<float>( MathF.E );
        }

        // -( a + b ) = - a + - b
        if ( !sr.HasFlag( SimplificationRules.Move_Negatives_Up ) )
        {
            if ( links[ 0 ] is PlenaryNode pn )
            {
                if ( pn.Value == Operator.MULT )
                    pn[ 0 ] = new UnaryNode( Operator.SBT, pn[ 0 ] );
                else
                    for ( int i = 0; i < pn.LinkCount; ++i ) pn[ i ] = new UnaryNode( Operator.SBT, pn[ i ] );
                return pn.Simplify( sr );
            }
        }

        return base.Simplify( sr );
    }
    public override int CompareTo( Node? o )
    {
        if ( o is Node<Operator> )
        {
            if ( o is UnaryNode )
                return GetHashCode() - o.GetHashCode();
            else
                return 1; //always put plenary operators before unary operators
        }
        return -1; //always put operators before numbers and variables
    }
}
public class PlenaryNode : OpNode
{
    public PlenaryNode( Operator Value, params Node[] links ) :
        base( Value, links )
    {
    }
    public override string? ToString()
    {
        //sort the nodes
        links.Sort();

        string s = "";
        for ( int i = 0; i < links.Count; ++i )
            s += " ( " + links[ i ] + " ) " + ( i + 1 != links.Count ? OperatorValue( Value ) : "" );
        while ( Regex.Match( s, "  " ).Success )
            s = Regex.Replace( s, "  ", " " );
        return s;
    }
    public void BinaryOpsToPlenaryOps()
    {
        for ( int i = 0; i < links.Count; ++i )
        {
            if ( links[ i ] is PlenaryNode no && Value == no.Value )
            {
                List<Node> LinkLinks = no.links;
                links.AddRange( LinkLinks );
                links.RemoveAt( i-- );
            }
        }
        foreach ( Node link in links )
        {
            if ( link is PlenaryNode PlenaryLink )
                PlenaryLink.BinaryOpsToPlenaryOps();
        }
    }
    public override Node Simplify( SimplificationRules sr )
    {
        PlenaryNode n = this;
        if ( Value == Operator.MULT )
        {
            for ( int i = 0; i < n.links.Count; ++i )
            {
                if ( n[ i ].IsOne() )
                    n.links.RemoveAt( i-- );
                if ( n[ i ].IsZero() )
                    return new Node<int>( 0 );
            }
            if ( sr.HasFlag( SimplificationRules.Isolate_Division ) )
            {
                //first, actually isolate division


                //if division is isolated, we only need to search at depth 1
                // a * / a = 1
                for ( int i = 0; i < n.LinkCount - 1; ++i )
                {
                    for ( int j = i + 1; j < n.LinkCount; ++j )
                    {
                        if ( n[ j ] is UnaryNode un && un.Value == Operator.DIV && un[ 0 ] == n[ i ] )
                        {
                            n.links.RemoveAt( j-- );
                            n.links.RemoveAt( i-- );
                        }
                    }
                }
            }
            if ( !links.Any() )
                return new Node<int>( 1 );
        }
        if ( Value == Operator.ADD )
        {
            for ( int i = 0; i < n.links.Count; ++i )
            {
                if ( n[ i ].IsZero() )
                    n.links.RemoveAt( i-- );
            }
            if ( !links.Any() )
                return new Node<int>( 0 );
        }
        if ( links.Count == 1 )
            return links[ 0 ].Simplify( sr );
        return n;
    }
    public override int CompareTo( Node? o )
    {
        if ( o is Node<Operator> )
        {
            if ( o is PlenaryNode )
                return GetHashCode() - o.GetHashCode();
            else
                return -1; //always put plenary operators before unary operators
        }
        return -1; //always put operators before numbers and variables
    }
}