namespace SymboMath;
using System.Text.RegularExpressions;
public static class Statics
{
    public static PlenaryOperator GetPlenaryOperator( string s )
    {
        return s switch 
        {
            "+" => PlenaryOperator.ADD,
            "*" => PlenaryOperator.MULT,
            _ => PlenaryOperator.NONE
        };
    }
    public static bool IsPlenaryOperator( string s, out PlenaryOperator po )
    {
        po = GetPlenaryOperator( s );
        return po != PlenaryOperator.NONE;
    }
    public static UnaryOperator GetUnaryOperator( string s )
    {
        return s switch
        {
            "-" => UnaryOperator.SBT,
            "/" => UnaryOperator.DIV,
            "exp" => UnaryOperator.EXP,
            "ln" => UnaryOperator.LN,
            _ => UnaryOperator.NONE
        };
    }
    public static bool IsUnaryOperator( string s, out UnaryOperator uo )
    {
        uo = GetUnaryOperator( s );
        return uo != UnaryOperator.NONE;
    }
    public static bool IsOperator( string s )
    {
        return IsUnaryOperator( s, out _ ) || IsPlenaryOperator( s, out _ );
    }
    private static string NegOpsToFuncs( string In )
    {
        Match m;
        //operator negatives are postfixed with '!'
        while ( ( m = Regex.Match( In, @"([^ ]*?) \- " ) ).Success )
        {
            string left = m.Result( "$1" );
            //negative is already a func not an operator, skip
            if ( left is "(" || IsOperator( left ) )
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
            if ( left is "(" || IsOperator( left ) )
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
    public static string FormatInfix( string Infix )
    {
        return ExpOpsToFuncs( DivOpsToFuncs( NegOpsToFuncs( Infix ) ) );
    }
    public static int OperatorRange( string[] Postfix )
    {
        return Postfix.Length - 1 - Postfix.ToList().LastIndexOf( Postfix.Last(), Postfix.Length - 2 );
    }
}