namespace SymboMath;
using System.Text.RegularExpressions;

public abstract class Node
{
    protected Node( Node[] links )
    {
        this.links = links;
    }
    private readonly Node[] links;
    public Node this[ int n ]
    {
        get => links[ n ];
        set => links[ n ] = value;
    }
    private static bool IsLeftAssoc( string Op )
    {
        return Op != "^";
    }
    private static int Precedence( string Op )
    {
        return Op switch
        {
            "+" or "-" => 0,
            "*" or "/" => 1,
            "^" => 2,
            _ => throw new InvalidOperationException( $"{Op} not an op" );
        };
    }
    private static Queue<string> InfixToPostfix( string Infix )
    {
        //turn - and / into functions instead of operators
        Infix = Regex.Replace( Infix, @"\- (.*?) ", a => a.Result( "+ ( - $1 ) " ) );
        Infix = Regex.Replace( Infix, @"\/ (.*?) ", a => a.Result( "+ ( - $1 ) " ) );


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
            else if ( InfN is "-" or "/" )
                Ops.Push( InfN );
            //operator
            else if ( InfN is "+" or "*" or "^" )
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
                    Ops.Push( InfN );
                }
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
            }
        }
    }
    public Node Parse( string Expression )
    {
        string[] ExpArr = Expression.Split( ' ' );
        Node[] ExpNodeArr = new Node[ ExpArr.Length ];
        for ( int n = 0; n < ExpArr.Length; ++n )
        {
            if ( int.TryParse( ExpArr[ n ], out int IntNode ) )
            {
                ExpNodeArr[ n ] = new Node<int>( IntNode );
                continue;
            }
        }
    }
}

public class Node<T> : Node
{
    public Node( T Value, params Node[] links ) : 
        base( links )
    {
        this.Value = Value;
        if ( Value is UnaryOperator )
            throw new NotSupportedException( "Cannot make Node<UnaryOperator>, please use UNode" );
    }
    public T Value;
    public static implicit operator T( Node<T> n ) => n.Value;
}
public class UNode : Node<UnaryOperator>
{
    public UNode( UnaryOperator Value, Node link ) :
        base( Value, link )
    {
    }
}