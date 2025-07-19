namespace VeryBasic.Runtime.Parsing;

public class Ast
{
    private string _code;
    
    private List<INode> _nodes;
    public List<INode> Nodes => _nodes;

    private int _index;
    
    private List<IToken> _tokens;

    public Ast(string code)
    {
        _code = code;
    }

    public List<INode> Parse()
    {
        List<IToken> tokens = new Tokenizer(_code).Tokenize();
        _tokens = tokens;
        _nodes = new List<INode>() {Expression()};
        return _nodes;
    }

    private IToken Advance()
    {
        return _tokens[_index++];
    }

    private bool Match(params SyntaxTokenType[] matches)
    {
        if (IsAtEnd()) return false;
        foreach (SyntaxTokenType match in matches) {
            if (Peek() is SyntaxToken token)
            {
                if (token.Type == match)
                {
                    Advance();
                    return true;
                }
            }
        }

        return false;
    }

    private bool Match(out IToken? Token, params Type[] matches)
    {
        IToken current = Peek();
        foreach (Type type in matches)
        {
            if (current.GetType() == type)
            {
                Token = current;
                Advance();
                return true;
            }
        }
        Token = null;
        return false;
    } 

    private IToken Previous()
    {
        return _tokens[_index - 1];
    }

    private bool IsAtEnd()
    {
        return _index >= _code.Length;
    }

    private IToken Peek()
    {
        return _tokens[_index];
    }
    
    // Node parsers

    private INode Expression()
    {
        return null;
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

public class ValueNode(Value value) : IExpressionNode
{
    public Value Value = value;
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
    NotEqual,
    LessThan,
    GreaterThan,
    LEq,
    GEq
}

public class BinaryOpNode(IExpressionNode left, BinOp op, IExpressionNode right) : IExpressionNode
{
    public IExpressionNode Left = left;
    public IExpressionNode Right = right;
    public BinOp Op = op;
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