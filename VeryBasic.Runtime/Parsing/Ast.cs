using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using static VeryBasic.Runtime.Parsing.SyntaxTokenType;

namespace VeryBasic.Runtime.Parsing;

public class Ast
{
    private string _code;
    
    private List<INode> _nodes;
    public List<INode> Nodes => _nodes;

    private int _index;
    
    private List<IToken> _tokens = [];

    public Ast(string code)
    {
        _code = code;
    }

    public List<INode> Parse()
    {
        List<IToken> tokens = new Tokenizer(_code).Tokenize();
        _tokens = tokens;
        return [Expression()];
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

    private bool Match([NotNullWhen(true)] out IToken? token, params Type[] matches)
    {
        IToken current = Peek();
        foreach (Type type in matches)
        {
            if (current.GetType() == type)
            {
                token = current;
                Advance();
                return true;
            }
        }
        token = null;
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
        return Logical();
    }

    private IExpressionNode Logical()
    {
        IExpressionNode expr = Equality();
        while (Match(And, Or))
        {
            SyntaxToken opToken = (SyntaxToken) Previous();
            IExpressionNode right = Equality();
            BinOp op = BinOp.And;
            switch (opToken.Type)
            {
                case And:
                    op = BinOp.And;
                    break;
                case Or:
                    op = BinOp.Or;
                    break;
            }
            expr = new BinaryOpNode(expr, op, right);
        }
        return expr;
    }

    private IExpressionNode Equality()
    {
        IExpressionNode expr = Comparison();
        while (Match(Equal, NotEqual))
        {
            SyntaxToken opToken = (SyntaxToken) Previous();
            IExpressionNode right = Comparison();
            BinOp op = BinOp.Equal;
            switch (opToken.Type)
            {
                case Equal:
                    op = BinOp.Equal;
                    break;
                case NotEqual:
                    op = BinOp.NotEqual;
                    break;
            }
            expr = new BinaryOpNode(expr, op, right);
        }
        return expr;
    }

    private IExpressionNode Comparison()
    {
        IExpressionNode expr = Term();
        while (Match(LessThan, GreaterThan, GEq, LEq))
        {
            SyntaxToken opToken = (SyntaxToken) Previous();
            IExpressionNode right = Term();
            BinOp op = BinOp.LessThan;
            switch (opToken.Type)
            {
                case LessThan:
                    op = BinOp.LessThan;
                    break;
                case LEq:
                    op = BinOp.LEq;
                    break;
                case GreaterThan:
                    op = BinOp.GreaterThan;
                    break;
                case GEq:
                    op = BinOp.GEq;
                    break;
            }
            expr = new BinaryOpNode(expr, op, right);
        }
        return expr;
    }

    private IExpressionNode Term()
    {
        IExpressionNode expr = Factor();
        while (Match(Plus, Minus))
        {
            SyntaxToken opToken = (SyntaxToken) Previous();
            IExpressionNode right = Factor();
            BinOp op = BinOp.Add;
            switch (opToken.Type)
            {
                case Plus:
                    op = BinOp.Add;
                    break;
                case Minus:
                    op = BinOp.Sub;
                    break;
            }
            expr = new BinaryOpNode(expr, op, right);
        }
        return expr;
    }

    private IExpressionNode Factor()
    {
        IExpressionNode expr = Unary();
        while (Match(Multiply, Divide))
        {
            SyntaxToken opToken = (SyntaxToken) Previous();
            IExpressionNode right = Unary();
            BinOp op = BinOp.Mul;
            switch (opToken.Type)
            {
                case Multiply:
                    op = BinOp.Mul;
                    break;
                case Divide:
                    op = BinOp.Div;
                    break;
            }
            expr = new BinaryOpNode(expr, op, right);
        }
        return expr;
    }

    private IExpressionNode Unary()
    {
        IExpressionNode expr;
        if (Match(Minus, Not))
        {
            SyntaxToken opToken = (SyntaxToken) Previous();
            IExpressionNode operand = Unary();
            UnaryOp op = UnaryOp.Negate;
            switch (opToken.Type)
            {
                case Minus:
                    op = UnaryOp.Negate;
                    break;
                case Not:
                    op = UnaryOp.Invert;
                    break;
            }
            expr = new UnaryOpNode(op, operand);
        }
        else
        {
            expr = Primary();
        }

        return expr;
    }

    private IExpressionNode Primary()
    {
        if (Match(Yes, No))
        {
            bool value = ((SyntaxToken)Peek()).Type switch
            {
                Yes => true,
                No => false,
            };
            return new ValueNode(
                new Value(
                    value
                    )
                );
        }

        if (Match(out IToken? tok1, typeof(StringToken)))
        {
            return new ValueNode(
                new Value(
                    ((StringToken)tok1).String
                )
            );
        }
        
        if (Match(out IToken? tok2, typeof(NumberToken)))
        {
            return new ValueNode(
                new Value(
                    ((NumberToken)tok2).Number
                )
            );
        }

        if (Match(The))
        {
            if (!Match(Result)) throw new Exception();
            return new TheResultNode();
        }
        
        throw new Exception();
    }
}

public interface INode {}

public class ProcCallNode : INode
{
    public string Name;
    public List<IExpressionNode> Args;
}

public interface IExpressionNode : INode
{
    // TODO: Move this into INode
    public T Accept<T>(IVisitor<T> visitor);
}

public class TheResultNode : IExpressionNode
{
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitTheResultNode(this);
    }
}

public class ValueNode(Value value) : IExpressionNode
{
    public Value Value = value;
    
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitValueNode(this);
    }
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
    
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitBinaryOpNode(this);
    }
}

public enum UnaryOp
{
    Negate,
    Invert
}

public class UnaryOpNode(UnaryOp op, IExpressionNode expr) : IExpressionNode
{
    public IExpressionNode Expr = expr;
    public UnaryOp Op = op;
    
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitUnaryOpNode(this);
    }
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