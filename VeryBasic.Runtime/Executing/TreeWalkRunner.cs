using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class TreeWalkRunner : IVisitor<Value>
{
    private List<INode> _ast;

    private Environment _env;
    
    private static readonly Value VBNull = new (new Value.Null());

    public TreeWalkRunner(Ast ast)
    {
        _ast = ast.Parse();
        _env = Environment.Default();
    }

    public void Run()
    {
        foreach (var node in _ast)
        {
            // TODO: Change this to just INode after adding Accept to interface INode
            Console.WriteLine(((IExpressionNode)node).Accept(this).AsObject);
        }
    }

    public Value VisitValueNode(ValueNode node)
    {
        return node.Value;
    }

    public Value VisitUnaryOpNode(UnaryOpNode node)
    {
        Value value = node.Expr.Accept(this);
        if (node.Op == UnaryOp.Invert)
        {
            if (value.Type == VBType.Boolean)
            {
                return new Value(!value.Get<bool>());
            }
            throw new Exception();
        }
        
        if (node.Op == UnaryOp.Negate)
        {
            if (value.Type == VBType.Number)
            {
                return new Value(-value.Get<double>());
            }
            
            throw new Exception();
        }
        
        throw new Exception();
    }

    public Value VisitBinaryOpNode(BinaryOpNode node)
    {
        Value left = node.Left.Accept(this);
        Value right = node.Right.Accept(this);
        if (node.Op == BinOp.Add)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() + right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Sub)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() - right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Mul)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() * right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Div)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() / right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.And)
        {
            if (left.Type == VBType.Boolean && right.Type == VBType.Boolean)
            {
                return new Value(left.Get<bool>() && right.Get<bool>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Or)
        {
            if (left.Type == VBType.Boolean && right.Type == VBType.Boolean)
            {
                return new Value(left.Get<bool>() || right.Get<bool>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Equal)
        {
            return new Value(left.Equals(right));
        }
        
        if (node.Op == BinOp.LessThan)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() < right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.GreaterThan)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() > right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.LEq)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() <= right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.GEq)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() >= right.Get<double>());
            }
            throw new Exception();
        }
        
        throw new Exception();
    }

    public Value VisitTheResultNode(TheResultNode node)
    {
        throw new NotImplementedException();
    }
}