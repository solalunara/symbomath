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
        a = a.Simplify( new() );
        b = b.Simplify( new() );
        return a.Equals( b );
    }
    public static bool operator !=( Node a, Node b ) => !( a == b );
    public abstract override bool Equals( object? obj );
    public abstract override int GetHashCode();
    public abstract float GetFloatValue();
    public abstract int CompareTo( Node? o );
    public abstract Node Copy();

    public static Node operator -( Node n ) => new SBTNode( n ).Simplify( new() );
    public static Node operator !( Node n ) => new DIVNode( n ).Simplify( new() );
    public static Node operator +( Node n1, Node n2 ) => new ADDNode( n1, n2 ).Simplify( new() );
    public static Node operator -( Node n1, Node n2 ) => new ADDNode( n1, -n2 ).Simplify( new() );
    public static Node operator *( Node n1, Node n2 ) => new MULTNode( n1, n2 ).Simplify( new() );
    public static Node operator /( Node n1, Node n2 ) => new MULTNode( n1, !n2 ).Simplify( new() );
    public static Node operator ^( Node n1, Node n2 ) => Exp( Ln( n1 ) * n2 ).Simplify( new() );
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
    public override bool IsZero() 
    {
        try { return GetFloatValue() == 0.0f; } catch { return false; }
    }
    public override bool IsOne() 
    {
        try { return GetFloatValue() == 1.0f; } catch { return false; }
    }
    public override int GetHashCode()
    {
        return ( Value ).GetHashCode();
    }
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
    public static implicit operator Node<T>( T i ) => new Node<T>( i );
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

    public override int GetHashCode()
    {
        return ( Value, links ).GetHashCode();
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
        return Simplify( sr, sr );
    }
    public Node Simplify( SimplificationRules Children, SimplificationRules This )
    {
        OpNode copy = (OpNode)this.Copy();
        for ( int i = 0; i < LinkCount; ++i )
            copy[ i ] = links[ i ].Simplify( Children );
        if ( copy is PlenaryNode pn )
            pn.BinaryOpsToPlenaryOps();
        return copy.SimplifyOpNode( This );
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
        for ( int i = 0; i < links.Count; ++i )
        {
            if ( links[ i ] is PlenaryNode PlenaryLink )
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
        SimplificationRules srChildren = sr;
        //prevent recursive loop
        srChildren.DistributeDivision = !sr.DistributeNegative;

        return base.Simplify( srChildren, sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        //if neg zero just return zero
        if ( links[ 0 ].IsZero() )
            return links[ 0 ];

        //if it's a literal number, negate it
        if ( links[ 0 ] is Node<int> ni )
            return (-ni.Value).n();
        if ( links[ 0 ] is Node<float> nf )
            return (-nf.Value).n();

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
                    pn[ 0 ] = -pn[ 0 ];
                else
                    for ( int i = 0; i < pn.LinkCount; ++i ) pn[ i ] = -pn[ i ];
                return pn.Simplify( sr );
            }
        }

        // - ln( x ) -> ln( / x )
        if ( sr.Ln == Mode.Condense && links[ 0 ] is LNNode ln )
            return Ln( 1.n() / ln[ 0 ] );

        // -/a = /-a if factoring division OR if distributing negative
        if ( ( sr.DistributeNegative || sr.FactorDivision ) && links[ 0 ] is DIVNode dn )
            return 1.n() / -dn[ 0 ];

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
        SimplificationRules srChildren = sr;
        //prevent recursive loop
        srChildren.DistributeNegative = !sr.DistributeDivision;

        srChildren.DistributeAddition = false;
        srChildren.Prefer = Preference.Multiplication;
        srChildren.Exp = Mode.Condense;
        srChildren.Ln = Mode.Condense;
        return base.Simplify( srChildren, sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        //throw exception if div by zero
        if ( links[ 0 ].IsZero() )
            throw new DivideByZeroException( $"Cannot divide by zero: {this}" );

        //if div by one just return one
        if ( links[ 0 ].IsOne() )
            return links[ 0 ];

        //if it's a literal number, div it
        if ( links[ 0 ] is Node<float> nf )
            return ( 1 / nf.Value ).n();

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
        }

        // / exp( a ) -> exp( -a )
        if ( sr.Exp == Mode.Condense && links[ 0 ] is EXPNode en )
            return Exp( -en[ 0 ] );

        // /-a = -/a if distributedivision or factornegative
        if ( ( sr.DistributeDivision || sr.FactorNegative ) && links[ 0 ] is SBTNode sn )
            return -( 1.n() / sn[ 0 ] );

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
        SimplificationRules srChildren = sr;
        srChildren.DistributeNegative = true;
        srChildren.DistributeDivision = true;
        srChildren.DistributeAddition = true;
        srChildren.Prefer = Preference.Addition;
        //srChildren.Exp = sr.PreferRepeatMult ? Mode.Expand : Mode.Condense;
        srChildren.Ln = Mode.Condense;
        return base.Simplify( srChildren, sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        //if it's a literal number, exp it
        try
        {
            return ( MathF.Exp( links[ 0 ].GetFloatValue() ) ).n();
        } catch { }

        //if it's a literal number, div it
        if ( links[ 0 ] is Node<float> nf )
            return MathF.Exp( nf.Value ).n();


        if ( sr.Exp == Mode.Expand )
        {
            // exp( -a ) = / exp( a )
            if ( links[ 0 ] is SBTNode sn )
                return 1.n() / Exp( sn[ 0 ] );

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
            return Exp( MathF.E.n() * exexp[ 0 ] );


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
        SimplificationRules srChildren = sr;
        srChildren.DistributeNegative = true;
        srChildren.DistributeDivision = false;
        srChildren.DistributeAddition = false;
        srChildren.Prefer = Preference.Multiplication;
        srChildren.Exp = Mode.Condense;
        return base.Simplify( srChildren, sr );
    }

    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        //if it's a literal number, ln it
        try
        {
            return ( MathF.Log( links[ 0 ].GetFloatValue() ) ).n();
        } catch { }

        if ( sr.Ln == Mode.Expand )
        {
            // ln( /a ) = - ln( a )
            if ( links[ 0 ] is DIVNode dn )
                return -Ln( dn[ 0 ] );

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
        SimplificationRules srChildren = sr;
        srChildren.DistributeNegative = true;
        srChildren.DistributeDivision = true;
        srChildren.DistributeAddition = true;
        srChildren.Prefer = Preference.Addition;
        srChildren.Ln = Mode.Expand;
        return base.Simplify( srChildren, sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        for ( int i = 0; i < links.Count; ++i )
        {
            if ( links[ i ].IsZero() )
                links.RemoveAt( i-- );
        }

        //add literal numbers
        if ( links.Where( n => 
        {
            try
            {
                n.GetFloatValue();
                return true;
            } catch { return false; }
        }).Any() )
        {
            int FirstFloatIndex = -1;
            int FirstIntIndex = -1;
            for ( int i = 0; i < links.Count; ++i )
            {
                if ( links[ i ] is Node<int> )
                {
                    if ( FirstIntIndex == -1 )
                        FirstIntIndex = i;
                    else
                    {
                        links[ FirstIntIndex ] = (((Node<int>)(links[ FirstIntIndex ])).Value + ((Node<int>)(links[ i ])).Value).n();
                        links.RemoveAt( i-- );
                    }
                }
                if ( links[ i ] is Node<float> )
                {
                    if ( FirstFloatIndex == -1 )
                        FirstFloatIndex = i;
                    else
                    {
                        links[ FirstFloatIndex ] = (((Node<float>)(links[ FirstFloatIndex ])).Value + ((Node<float>)(links[ i ])).Value).n();
                        links.RemoveAt( i-- );
                    }
                }
            }
        }

        if ( !links.Any() )
            return 0.n();
        if ( links.Count == 1 )
            return links[ 0 ];


        // a + - a = 0
        for ( int i = 0; i < LinkCount - 1; ++i )
        {
            bool DontIncrementI = false;
            for ( int j = 0; j < LinkCount; ++j )
            {
                if ( i == j ) continue;

                if ( links[ j ] is SBTNode snj && snj[ 0 ] == links[ i ] )
                {
                    if ( j > i )
                    {
                        links.RemoveAt( j-- );
                        links.RemoveAt( i );
                    }
                    else
                    {
                        links.RemoveAt( i );
                        links.RemoveAt( j-- );
                    }
                    DontIncrementI = true;
                }
            }
            if ( DontIncrementI )
                --i;
        }

        // ln( a ) + ln( b ) -> ln( a * b )
        if ( sr.Ln == Mode.Condense )
        {
            for ( int i = 0; i < LinkCount - 1; ++i )
            {
                for ( int j = 0; j < LinkCount; ++j )
                {
                    if ( i == j ) continue;

                    if ( links[ j ] is LNNode lnj && links[ i ] is LNNode lni ) 
                    {
                        links[ i ] = Ln( lnj[ 0 ] * lni[ 0 ] );
                        if ( i > j ) --i;
                        links.RemoveAt( j-- );
                    }
                }
            }
        }

        if ( !links.Any() )
            return 0.n();
        if ( links.Count == 1 )
            return links[ 0 ];

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
        SimplificationRules srChildren = sr;
        srChildren.DistributeNegative = false;
        srChildren.DistributeDivision = true;
        srChildren.DistributeAddition = false;
        srChildren.Prefer = Preference.Multiplication;
        srChildren.Exp = Mode.Expand;
        return base.Simplify( srChildren, sr );
    }
    protected override Node SimplifyOpNode( SimplificationRules sr )
    {
        for ( int i = 0; i < links.Count; ++i )
        {
            if ( links[ i ].IsOne() )
                links.RemoveAt( i-- );
            else if ( links[ i ].IsZero() )
                return new Node<int>( 0 );
        }

        // multiply literal numbers
        if ( links.Where( n => 
        {
            try
            {
                n.GetFloatValue();
                return true;
            } catch { return false; }
        }).Any() )
        {
            int FirstFloatIndex = -1;
            int FirstIntIndex = -1;
            for ( int i = 0; i < links.Count; ++i )
            {
                if ( links[ i ] is Node<int> ni )
                {
                    if ( FirstIntIndex == -1 )
                        FirstIntIndex = i;
                    else
                    {
                        links[ FirstIntIndex ] = (((Node<int>)(links[ FirstIntIndex ])).Value * ((Node<int>)(links[ i ])).Value).n();
                        links.RemoveAt( i-- );
                    }
                }
                if ( links[ i ] is Node<float> nf )
                {
                    if ( FirstFloatIndex == -1 )
                        FirstFloatIndex = i;
                    else
                    {
                        links[ FirstFloatIndex ] = (((Node<float>)(links[ FirstFloatIndex ])).Value * ((Node<float>)(links[ i ])).Value).n();
                        links.RemoveAt( i-- );
                    }
                }
            }
        }

        if ( !links.Any() )
            return new Node<int>( 1 );
        if ( links.Count == 1 )
            return links[ 0 ];

        // a * n = a + a + a... n times
        if ( sr.Prefer == Preference.Addition )
        {
            for ( int i = 0; i < LinkCount; ++i )
            {
                if ( links[ i ] is Node<int> ni )
                {
                    OpNode a = (OpNode)Copy();
                    a.links.RemoveAt( i );
                    Node[] links = new Node[ ni.Value ];
                    for ( int j = 0; j < ni.Value; ++j )
                        links[ j ] = a.Copy();
                    return new ADDNode( links ).Simplify( sr );
                }
            }
        }

        // - a * - b = a * b, even if we're not factoring negatives
        for ( int i = 0; i < links.Count; ++i )
        {
            for ( int j = 0; j < links.Count; ++j )
            {
                if ( i == j ) continue;

                if ( links[ i ] is SBTNode sn1 && links[ j ] is SBTNode sn2 )
                {
                    links[ i ] = sn1[ 0 ];
                    links[ j ] = sn2[ 0 ];
                }
            }
        }

        // a * - b = -( a * b )
        if ( sr.FactorNegative )
        {
            for ( int i = 0; i < links.Count; ++i )
            {
                if ( links[ i ] is SBTNode sn )
                {
                    links[ i ] = sn[ 0 ];
                    return new SBTNode( this ).Simplify( sr );
                }
            }
        }

        // / a * / b = / ( a * b )
        if ( sr.FactorDivision )
        {
            if ( !links.Where( n => n is not DIVNode ).Any() )
                return new DIVNode( this ).Simplify( sr );

            DIVNode dn = new DIVNode( new MULTNode( links.Where( n => n is DIVNode ).ToArray() ) );
            MULTNode mn = new MULTNode( links.Where( l => l is not DIVNode ).Append( dn ).ToArray() );
            if ( ((OpNode)(dn[ 0 ])).LinkCount > 0 )
                return mn.Simplify( sr );
        }

        // a * / a = 1
        for ( int i = 0; i < LinkCount - 1; ++i )
        {
            bool DontIncrementI = false;
            for ( int j = 0; j < LinkCount; ++j )
            {
                if ( i == j ) continue;

                if ( links[ j ] is DIVNode dnj && dnj[ 0 ] == links[ i ] )
                {
                    if ( j > i )
                    {
                        links.RemoveAt( j-- );
                        links.RemoveAt( i );
                    }
                    else
                    {
                        links.RemoveAt( i-- );
                        links.RemoveAt( j-- );
                    }
                    DontIncrementI = true;
                }
            }
            if ( DontIncrementI )
                --i;
        }

        // exp( a ) * exp( b ) -> exp( a + b )
        if ( sr.Exp == Mode.Condense )
        {
            for ( int i = 0; i < LinkCount - 1; ++i )
            {
                for ( int j = 0; j < LinkCount; ++j )
                {
                    if ( i == j ) continue;

                    if ( links[ j ] is EXPNode enj && links[ i ] is EXPNode eni ) 
                    {
                        // exp( a ) * exp( b ) -> exp( a + b )
                        links[ i ] = Exp( enj[ 0 ] + eni[ 0 ] );
                        if ( i > j ) --i;
                        links.RemoveAt( j-- );
                    }
                }
            }
        }

        
        if ( !links.Any() )
            return new Node<int>( 1 );
        if ( links.Count == 1 )
            return links[ 0 ];

        return this.Copy();
    }
}