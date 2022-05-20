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
        Infix = FormatInfix( Infix );

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
                if ( IsUnaryOperator( Ops.Peek(), out _ ) )
            }
        }
        while ( Ops.Any() )
            Output.Enqueue( Ops.Pop() );
        return Output;
    }
    public Node Parse( string[] Postfix )
    {
        Node[] ExpNodeArr = new Node[ Postfix.Length ];
        for ( int n = Postfix.Length; n >= 0; --n )
        {
            if ( int.TryParse( Postfix[ n ], out int IntNode ) )
            {
                ExpNodeArr[ n ] = new Node<int>( IntNode );
                continue;
            }
            if ( float.TryParse( Postfix[ n ], out float FloatNode ) )
            {
                ExpNodeArr[ n ] = new Node<float>( FloatNode );
                continue;
            }
            if ( IsUnaryOperator( Postfix[ n ], out UnaryOperator uo ) )
            {
                ExpNodeArr[ n ] = new Node<UnaryOperator>( uo, Parse(  ) );
                continue;
            }
            if ( IsPlenaryOperator( Postfix[ n ], out PlenaryOperator po ) )
            {
                int Index = n - 1;
                while ( Postfix[ n ] != Postfix[ Index ] )
                {
                    --Index;
                }
                ++Index;
                ExpNodeArr[ n ] = new Node<PlenaryOperator>( po, Parse( Postfix[ Index..n ] ) );
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