namespace VeryBasic.Runtime.Parsing;

public class Ast
{
    private string _code;
    
    private List<INode> _nodes;
    public List<INode> Nodes => _nodes;

    public Ast(string code)
    {
        _code = code;
    }

    public void Parse()
    {
        List<IToken> tokens = new Tokenizer(_code).Tokenize();
        Console.WriteLine(string.Join(" ", tokens));
    }
}

public interface INode {}

public class ProcCallNode : INode
{
    public string Name;
    public List<IExpressionNode> Args;
}

public interface IExpressionNode : INode {}

public class TheResultNode : IExpressionNode {}

public class ValueNode : IExpressionNode
{
    private Value _value;
}

public enum BinOp
{
    Add,
    Sub,
    Mul,
    Div,
    And,
    Or,
    Equal,
    NotEqual
}

public class BinaryOpNode : IExpressionNode
{
    public IExpressionNode Left;
    public IExpressionNode Right;
    public BinOp Op;
}

public enum UnaryOp
{
    Negate,
    Invert
}

public class UnaryOpNode : IExpressionNode
{
    public IExpressionNode Expr;
    public UnaryOp Op;
}

public class IfNode : INode
{
    public IExpressionNode Condition;
    public List<INode> Then;
    public List<INode>? Else;
}

public class VarDecNode : INode
{
    public string Name;
    public IExpressionNode? Value;
}

public class VarSetNode : INode
{
    public string Name;
    public IExpressionNode Value;
}

public class CalculateNode : INode
{
    public IExpressionNode Expr;
}

public class RepeatLoopNode : INode
{
    public IExpressionNode Times;
    public List<INode> Loop;
}

public class WhileLoopNode : INode
{
    public IExpressionNode Condition;
    public List<INode> Loop;
}