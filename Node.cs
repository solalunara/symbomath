
abstract class Node
{
    protected Node( Node[] links )
    {
        this.links = links;
    }
    private Node[] links;
}

class Node<T> : Node
{
    public Node( T Value, params Node[] links ) : 
        base( links )
    {
        this.Value = Value;
    }
    T Value;
}