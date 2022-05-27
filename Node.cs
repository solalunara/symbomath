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
            return OpNode.Parse( uo, ParsePostfix( Postfix[ ..^1 ] ) );
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
            PlenaryNode pn = (PlenaryNode)OpNode.Parse( po, ParsePostfix( arg1 ), ParsePostfix( arg2 ) ); 
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
        a.Simplify( new() );
        b.Simplify( new() );
        return a.Equals( b );
    }
    public static bool operator !=( Node a, Node b ) => !( a == b );
    public abstract override bool Equals( object? obj );
    public override int GetHashCode()
    {
        Node n = this;
        n.Simplify( new() );
        return ( n.ToString() ?? "" ).GetHashCode();
    }
    public abstract float GetFloatValue();
    public abstract int CompareTo( Node? o );
    public abstract Node Copy();
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
    public override Node Simplify( SimplificationRules sr ) => this;
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
    public override Node Copy()
    {
        return new Node<T>( Value );
    }
}

public abstract class OpNode : Node<Operator>
{
    protected OpNode( Operator val, params Node[] links ) :
        base( val )
    {
        this.links = links.ToList();
    }
    public static OpNode Parse( Operator op, params Node[] links )
    {
        return op switch
        {
            Operator.SBT => new SBTNode( links[ 0 ] ),
            Operator.DIV => new DIVNode( links[ 0 ] ),
            Operator.EXP => new EXPNode( links[ 0 ] ),
            Operator.LN => new LNNode( links[ 0 ] ),
            Operator.ADD => new ADDNode( links ),
            Operator.MULT => new MULTNode( links ),
            _ => throw new ArgumentException( $"Operator {op} is not a operator so it can't be parsed to one" )
        };
    }
    public List<Node> links;
    public int LinkCount { get => links.Count; }
    public Node this[ int n ] { get => links[ n ]; set => links[ n ] = value; }
    public override bool IsZero() => false;
    public override bool IsOne() => false;
    public override float GetFloatValue() => throw new InvalidOperationException( "Operator does not have float value" );
    public abstract override int CompareTo( Node? o );
    public override Node Simplify( SimplificationRules sr )
    {
        OpNode copy = (OpNode)this.Copy();
        for ( int i = 0; i < LinkCount; ++i )
            copy[ i ] = links[ i ].Simplify( sr );
        if ( copy is PlenaryNode pn )
            pn.BinaryOpsToPlenaryOps();
        return copy.SimplifyOpNode( sr );
    }
    //SimplifyOpNode should always be running in a copy, so we can modify links directly
    protected abstract Node SimplifyOpNode( SimplificationRules sr );
    public override Node Copy()
    {
        Node[] links = new Node[ this.links.Count ];
        for ( int i = 0; i < links.Length; ++i )
            links[ i ] = this.links[ i ].Copy();
        return OpNode.Parse( Value, links );
    }
}
public abstract class UnaryNode : OpNode
{
    public UnaryNode( Operator Value, Node link ) :
        base( Value, link )
    {
    }
    public override string? ToString() 
    {
        string s;
        if ( links[ 0 ] is OpNode )
        {
            s = OperatorValue( Value ) + " ( " + links[ 0 ].ToString() + " )";
            while ( Regex.Match( s, "  " ).Success )
                s = Regex.Replace( s, "  ", " " );
        }
        else
        {
            s = OperatorValue( Value ) + " " + links[ 0 ].ToString();
            while ( Regex.Match( s, "  " ).Success )
                s = Regex.Replace( s, "  ", " " );
        }
        return s;
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
public abstract class PlenaryNode : OpNode
{
    public PlenaryNode( Operator Value, params Node[] links ) :
        base( Value, links )
    {
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
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        if ( links.Count == 1 )
            return links[ 0 ].Simplify( sr );
        return this;
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

public class SBTNode : UnaryNode
{
    public SBTNode( Node link ) :
        base( Operator.SBT, link )
    {
    }
    public override Node Simplify( SimplificationRules sr )
    {
        sr.DistributeDivision = !sr.DistributeNegative; //prevent recursive loop
        return base.Simplify( sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        // - - a = a
        if ( links[ 0 ] is SBTNode sbtlink )
            return sbtlink[ 0 ].Simplify( sr );

        // -( a + b ) = - a + - b
        if ( sr.DistributeNegative )
        {
            if ( links[ 0 ] is PlenaryNode )
            {
                PlenaryNode pn = (PlenaryNode)links[ 0 ].Copy();
                if ( pn.Value == Operator.MULT )
                    pn[ 0 ] = new SBTNode( pn[ 0 ] );
                else
                    for ( int i = 0; i < pn.LinkCount; ++i ) pn[ i ] = new SBTNode( pn[ i ] );
                return pn.Simplify( sr );
            }
            // -/a = /-a
            // WARNING: If both DistributeDivision and DistributeNegative are set
            //          this will result in infinite recursion
            //          handle this? nah it's fine tm
            if ( links[ 0 ] is DIVNode dn )
                return new DIVNode( new SBTNode( dn[ 0 ] ) );
        }

        return this.Copy();
    }
}
public class DIVNode : UnaryNode
{
    public DIVNode( Node link ) :
        base( Operator.DIV, link )
    {
    }
    public override Node Simplify( SimplificationRules sr )
    {
        sr.DistributeNegative = !sr.DistributeDivision; //prevent recursive loop
        sr.DistributeAddition = false;
        sr.Prefer = Preference.Multiplication;
        sr.Exp = Mode.Condense;
        sr.Ln = Mode.Condense;
        return base.Simplify( sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        //throw exception if div by zero
        if ( links[ 0 ].IsZero() )
            throw new DivideByZeroException( $"Cannot divide by zero: {this}" );

        //if div by one just return one
        if ( links[ 0 ].IsOne() )
            return links[ 0 ];

        // / / a = a
        if ( links[ 0 ] is DIVNode divlink )
            return divlink[ 0 ].Simplify( sr );

        if ( sr.DistributeDivision && links[ 0 ] is OpNode )
        {
            // /( a * b ) = /a * /b
            if ( links[ 0 ] is MULTNode mn )
            {
                DIVNode[] links = new DIVNode[ mn.LinkCount ];
                for ( int i = 0; i < mn.LinkCount; ++i )
                    links[ i ] = new DIVNode( mn[ i ] );
                return new MULTNode( links ).Simplify( sr );
            }
            // /-a = -/a
            // WARNING: If both DistributeDivision and DistributeNegative are set
            //          this will result in infinite recursion
            //          handle this? nah it's fine tm
            if ( links[ 0 ] is SBTNode sn )
                return new SBTNode( new DIVNode( sn[ 0 ] ) );
        }

        return this.Copy();
    }
}
public class EXPNode : UnaryNode
{
    public EXPNode( Node link ) :
        base( Operator.EXP, link )
    {
    }
    public override Node Simplify( SimplificationRules sr )
    {
        sr.DistributeNegative = true;
        sr.DistributeDivision = true;
        sr.DistributeAddition = true;
        sr.PreferRepeatMult = false;
        sr.Prefer = Preference.Addition;
        sr.Exp = Mode.Condense;
        sr.Ln = Mode.Condense;
        return base.Simplify( sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        // exp( a * n ) = exp( a ) * exp( a ) * exp( a )... n times
        if ( sr.PreferRepeatMult )
        {
            if ( links[ 0 ] is MULTNode mn )
            {
                for ( int i = 0; i < mn.LinkCount; ++i )
                {
                    if ( mn[ i ] is Node<int> ni )
                    {
                        OpNode MNMinusNI = (OpNode)mn.Copy();
                        MNMinusNI.links.RemoveAt( i );
                        EXPNode[] links = new EXPNode[ ni.Value ];
                        for ( int j = 0; j < ni.Value; ++j )
                            links[ j ] = new EXPNode( MNMinusNI );
                        return new MULTNode( links ).Simplify( sr );
                    }
                }
            }
        }

        if ( sr.Exp == Mode.Expand )
        {
            // exp( -a ) = / exp( a )
            if ( links[ 0 ] is SBTNode sn )
                return new DIVNode( new EXPNode( sn[ 0 ] ) ).Simplify( sr );

            // exp( a + b ) = exp( a ) * exp( b )
            if ( links[ 0 ] is ADDNode an )
            {
                EXPNode[] links = new EXPNode[ an.LinkCount ];
                for ( int i = 0; i < an.LinkCount; ++i )
                    links[ i ] = new EXPNode( an[ i ] );
                return new MULTNode( links ).Simplify( sr );
            }
        }

        // exp( exp( a ) ) = exp( e * a )
        if ( links[ 0 ] is EXPNode exexp )
            return new EXPNode( new MULTNode( new Node<float>( MathF.E ), exexp[ 0 ] ) ).Simplify( sr );


        // exp( ln( a ) ) = a
        if ( links[ 0 ] is LNNode lnexp )
            return lnexp[ 0 ].Simplify( sr );

        return this.Copy();
    }
}
public class LNNode : UnaryNode
{
    public LNNode( Node link ) :
        base( Operator.LN, link )
    {
    }

    public override Node Simplify( SimplificationRules sr )
    {
        sr.DistributeNegative = true;
        sr.DistributeDivision = false;
        sr.DistributeAddition = false;
        sr.PreferRepeatMult = true;
        sr.Prefer = Preference.Multiplication;
        sr.Exp = Mode.Expand;
        return base.Simplify( sr );
    }

    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        if ( sr.Ln == Mode.Expand )
        {
            // ln( /a ) = - ln( a )
            if ( links[ 0 ] is DIVNode dn )
                return new SBTNode( new LNNode( dn[ 0 ] ) ).Simplify( sr );

            // ln( a * b ) = ln( a ) + ln( b )
            if ( links[ 0 ] is MULTNode mn )
            {
                LNNode[] links = new LNNode[ mn.LinkCount ];
                for ( int i = 0; i < mn.LinkCount; ++i )
                    links[ i ] = new LNNode( mn[ i ] );
                return new ADDNode( links ).Simplify( sr );
            }
        }

        // ln( exp( a ) ) = a
        if ( links[ 0 ] is EXPNode lnexp )
            return lnexp[ 0 ].Simplify( sr );

        return this.Copy();
    }
}
public class ADDNode : PlenaryNode
{
    public ADDNode( params Node[] links ) :
        base( Operator.ADD, links )
    {
    }
    public override string? ToString()
    {
        //sort the nodes
        links.Sort();

        string s = "";
        for ( int i = 0; i < links.Count; ++i )
        {
            if ( links[ i ] is OpNode )
                s += " ( " + links[ i ] + " ) " + ( i + 1 != links.Count ? OperatorValue( Value ) : "" );
            else
                s += " " + links[ i ] + " " + ( i + 1 != links.Count ? OperatorValue( Value ) : "" );
        }
        while ( Regex.Match( s, "  " ).Success )
            s = Regex.Replace( s, "  ", " " );
        return s;
    }
    public override Node Simplify( SimplificationRules sr )
    {
        sr.DistributeNegative = true;
        sr.DistributeDivision = true;
        sr.DistributeAddition = true;
        sr.Prefer = Preference.Addition;
        sr.Ln = Mode.Expand;
        return base.Simplify( sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        for ( int i = 0; i < links.Count; ++i )
        {
            try
            {
                if ( links[ i ].IsZero() )
                    links.RemoveAt( i-- );
            } catch { } //not a number, guarenteed nonzero
        }
        // a + - a = 0
        for ( int i = 0; i < LinkCount - 1; ++i )
        {
            bool DontIncrementI = false;
            for ( int j = i + 1; j < LinkCount; ++j )
            {
                if ( links[ j ] is SBTNode snj && snj[ 0 ] == links[ i ] )
                {
                    links.RemoveAt( j-- );
                    links.RemoveAt( i );
                    DontIncrementI = true;
                }
                else if ( links[ i ] is SBTNode sni && sni[ 0 ] == links[ j ] )
                {
                    links.RemoveAt( j-- );
                    links.RemoveAt( i );
                    DontIncrementI = true;
                }
            }
            if ( DontIncrementI )
                --i;
        }
        if ( !links.Any() )
            return new Node<int>( 0 );

        return this.Copy();
    }
}
public class MULTNode : PlenaryNode
{
    public MULTNode( params Node[] links ) :
        base( Operator.MULT, links )
    {
    }
    public override string? ToString()
    {
        //sort the nodes
        links.Sort();

        string s = "";
        for ( int i = 0; i < links.Count; ++i )
        {
            if ( links[ i ] is OpNode )
                s += " ( " + links[ i ] + " ) " + ( i + 1 != links.Count ? OperatorValue( Value ) : "" );
            else
                s += " " + links[ i ] + " " + ( i + 1 != links.Count ? OperatorValue( Value ) : "" );
        }
        while ( Regex.Match( s, "  " ).Success )
            s = Regex.Replace( s, "  ", " " );
        return s;
    }
    public override Node Simplify( SimplificationRules sr )
    {
        sr.DistributeNegative = true;
        sr.DistributeDivision = true;
        sr.DistributeAddition = false;
        sr.Prefer = Preference.Multiplication;
        sr.Exp = Mode.Expand;
        return base.Simplify( sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        for ( int i = 0; i < links.Count; ++i )
        {
            try
            {
                if ( links[ i ].IsOne() )
                    links.RemoveAt( i-- );
                if ( links[ i ].IsZero() )
                    return new Node<int>( 0 );
            } catch { } //not a number, no need to test for 0/1
        }
        // a * / a = 1
        for ( int i = 0; i < LinkCount - 1; ++i )
        {
            bool DontIncrementI = false;
            for ( int j = i + 1; j < LinkCount; ++j )
            {
                if ( links[ j ] is DIVNode dnj && dnj[ 0 ] == links[ i ] )
                {
                    links.RemoveAt( j-- );
                    links.RemoveAt( i );
                    DontIncrementI = true;
                }
                else if ( links[ i ] is DIVNode dni && dni[ 0 ] == links[ j ] )
                {
                    links.RemoveAt( j-- );
                    links.RemoveAt( i );
                    DontIncrementI = true;
                }
            }
            if ( DontIncrementI )
                --i;
        }
        if ( !links.Any() )
            return new Node<int>( 1 );
        
        return this.Copy();
    }
}