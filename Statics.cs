namespace SymboMath;
using System.Text.RegularExpressions;
public static class Statics
{
    public static Operator GetOperator( string s )
    {
        return s switch
        {
            "-" => Operator.SBT,
            "/" => Operator.DIV,
            "exp" => Operator.EXP,
            "ln" => Operator.LN,
            "+" => Operator.ADD,
            "*" => Operator.MULT,
            _ => Operator.NONE
        };
    }
    public static bool IsUnaryOperator( Operator o )
    {
        return ( Operator.UnaryOperator & o ) != 0;
    }
    public static bool IsUnaryOperator( string s, out Operator o )
    {
        o = GetOperator( s );
        return ( Operator.UnaryOperator & o ) != 0;
    }
    public static bool IsPlenaryOperator( Operator o )
    {
        return ( Operator.PlenaryOperator & o ) != 0;
    }
    public static bool IsPlenaryOperator( string s, out Operator o )
    {
        o = GetOperator( s );
        return ( Operator.PlenaryOperator & o ) != 0;
    }
    public static bool IsOperator( string s, out Operator o )
    {
        o = GetOperator( s );
        return o != Operator.NONE;
    }
    private static string NegOpsToFuncs( string In )
    {
        Match m;
        //operator negatives are postfixed with '!'
        while ( ( m = Regex.Match( In, @"([^ ]*?) \- " ) ).Success )
        {
            string left = m.Result( "$1" );
            //negative is already a func not an operator, skip
            if ( left is "(" || IsOperator( left, out _ ) )
            {
                In = In[ ..m.Index ] + m.Result( "$1 -! " ) + In[ ( m.Index + m.Length ).. ];
                continue;
            }
            //otherwise swap "x - y" to "x + - y"
            In = In[ ..m.Index ] + m.Result( "$1 + -! " ) + In[ ( m.Index + m.Length ).. ];
        }
        //now that we're done change all "-!"'s to "-"
        return Regex.Replace( In, @"\-\!", "-" );
    }
    private static string DivOpsToFuncs( string In )
    {
        Match m;
        //though unlikely to occur naturally, operator divs are postfixed with '!'
        while ( ( m = Regex.Match( In, @"([^ ]*?) \/ " ) ).Success )
        {
            string left = m.Result( "$1" );
            //div is already a func not an operator, skip
            if ( left is "(" || IsOperator( left, out _ ) )
            {
                In = In[ ..m.Index ] + m.Result( "$1 /! " ) + In[ ( m.Index + m.Length ).. ];
                continue;
            }
            //otherwise swap "x / y" to "x * / y"
            In = In[ ..m.Index ] + m.Result( "$1 * /! " ) + In[ ( m.Index + m.Length ).. ];
        }
        //now that we're done change all "/!"s to "/"s
        return Regex.Replace( In, @"\/\!", "/" );
    }
    private static string ExpOpsToFuncs( string In )
    {
        Match m;
        while ( ( m = Regex.Match( In, @"([^ ]*?) \^ ([^ ]*?)" ) ).Success )
        {
            string Base = m.Result( "$1" );
            string Exponent = m.Result( "$2" );
            int MatchIndex = m.Index;
            int MatchLength = m.Length;
            //if left is end perim, we need to figure out the actual base
            if ( Base is ")" )
            {
                //find the matching open perim
                int Count = 1;
                int Index = MatchIndex - 1;
                for ( ; Index >= 0 && Count != 0; --Index )
                {
                    switch ( In[ Index ] )
                    {
                        case ')':
                            ++Count;
                            break;
                        case '(':
                            --Count;
                            break;
                    }
                    ++MatchLength;
                }
                Base = In[ Index..( MatchIndex + Base.Length ) ];
                MatchIndex = Index;
            }
            //do the same for the exponent (if it's a start perim, figure out the actual exponent)
            if ( Exponent is "(" )
            {
                //find the matching close perim
                int Count = 1;
                int Index = MatchIndex + MatchLength;
                for ( ; Index >= 0 && Count != 0; --Index )
                {
                    switch ( In[ Index ] )
                    {
                        case ')':
                            --Count;
                            break;
                        case '(':
                            ++Count;
                            break;
                    }
                    ++MatchLength;
                }
                Exponent = In[ ( m.Index + m.Result( "$1" ).Length + 1 )..Index ];
            }
            //if base is not e, "ln base * " needs to go in the exp. Otherwise nothing needs to be added
            Base = Base != "e" ? "ln " + Base : "";
            //turn x ^ y into exp( ln x * y )
            In = In[ ..MatchIndex ] + "exp ( " + Base + Exponent + ") " + In[ ( MatchIndex + MatchLength ).. ];
        }
        //now that we're done change all "/!"s to "/"s
        return Regex.Replace( In, @"\/\!", "/" );
    }
    public static string FormatExpression( string Infix )
    {
        return ExpOpsToFuncs( DivOpsToFuncs( NegOpsToFuncs( Infix ) ) );
    }
    public static int OperatorRange( string[] Postfix )
    {
        return Postfix.Length - 1 - Postfix.ToList().LastIndexOf( Postfix.Last(), Postfix.Length - 2 );
    }
    public static bool IsLeftAssoc( string Op )
    {
        return Op != "^";
    }
    public static int Precedence( string Op )
    {
        return Precedence( GetOperator( Op ) );
    }
    public static int Precedence( Operator Op )
    {
        return Op switch
        {
            Operator.ADD => 0,
            Operator.MULT => 1,
            _ => throw new InvalidOperationException( $"{Op} not a plenary operator" )
        };
    }
    public static Queue<string> InfixToPostfix( string Infix )
    {
        Infix = FormatExpression( Infix );

        Queue<string> Output = new();
        Stack<string> Ops = new();

        string[] InfArr = Infix.Split( ' ' );
        for ( int n = 0; n < InfArr.Length; ++n )
        {
            string InfN = InfArr[ n ];
            //number
            if ( int.TryParse( InfN, out _ ) || float.TryParse( InfN, out _ ) )
                Output.Enqueue( InfN );
            //function
            else if ( IsUnaryOperator( InfN, out _ ) )
                Ops.Push( InfN );
            //operator
            else if ( IsPlenaryOperator( InfN, out _ ) )
            {
                while ( 
                    Ops.Any() && Ops.Peek() != "(" &&
                        ( 
                            Precedence( Ops.Peek() ) > Precedence( InfN ) || 
                            ( 
                                IsLeftAssoc( InfN ) && Precedence( Ops.Peek() ) >= Precedence( InfN ) 
                            ) 
                        )
                    )
                {
                    Output.Enqueue( Ops.Pop() );
                }
                Ops.Push( InfN );
            }
            //left perim
            else if ( InfN is "(" )
                Ops.Push( InfN );
            //right perim
            else if ( InfN is ")" )
            {
                while ( Ops.Peek() != "(" )
                {
                    Output.Enqueue( Ops.Pop() );
                }
                Ops.Pop();
                if ( IsUnaryOperator( Ops.Peek(), out _ ) )
                    Output.Enqueue( Ops.Pop() );
            }
            else if ( InfN.Any() )
                Output.Enqueue( InfN );
        }
        while ( Ops.Any() )
            Output.Enqueue( Ops.Pop() );
        return Output;
    }
}