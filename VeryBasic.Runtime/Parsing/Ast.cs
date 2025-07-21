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
        return Program();
    }

    private IToken Advance()
    {
        return _tokens[_index++];
    }

    private void Consume(SyntaxTokenType token, string errorMsg)
    {
        if (!Match(token)) throw new Exception(errorMsg);
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
    
    private bool Check(params SyntaxTokenType[] matches)
    {
        if (IsAtEnd()) return false;
        foreach (SyntaxTokenType match in matches) {
            if (Peek() is SyntaxToken token)
            {
                if (token.Type == match)
                {
                    // Don't advance, only look.
                    return true;
                }
            }
        }

        return false;
    }

    private bool Match([NotNullWhen(true)] out IToken? token, params Type[] matches)
    {
        if (IsAtEnd())
        {
            token = null;
            return false;
        }
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
    
    private bool Check(params Type[] matches)
    {
        if (IsAtEnd())
        {
            return false;
        }
        IToken current = Peek();
        foreach (Type type in matches)
        {
            if (current.GetType() == type)
            {
                // Don't advance; only look.
                return true;
            }
        }
        return false;
    }

    private IToken Previous()
    {
        return _tokens[_index - 1];
    }

    private bool IsAtEnd()
    {
        return _index > _tokens.Count - 1;
    }

    private IToken Peek()
    {
        return _tokens[_index];
    }
    
    private IToken Peek(int offset)
    {
        return _tokens[_index + offset];
    }
    
    // Node parsers

    private List<INode> Program()
    {
        List<INode> statements = new List<INode>();
        while (!IsAtEnd())
        {
            statements.Add(Statement());
        }
        return statements;
    }

    private VBType Type()
    {
        if (Match(out var typeName, typeof(IdentToken)))
        {
            return ((IdentToken)typeName).Name switch
            {
                "number" => VBType.Number,
                "string" => VBType.String,
                "boolean" => VBType.Boolean,
                _ => throw new Exception()
            };
        }
        throw new Exception();
    }

    private INode Statement()
    {
        if (Match(Declare))
        {
            return VarDec();
        }

        if (Match(Update))
        {
            return VarSet();
        }

        if (Check(typeof(IdentToken)))
        {
            return ProcCall();
        }

        if (Match(If))
        {
            return IfStmt();
        }
        throw new Exception();
    }

    private INode VarDec()
    {
        Consume(Variable, "You missed a word: 'variable'.");
        if (
            Match(out var nameToken, typeof(IdentToken))
        )
        {
            Consume(Comma, "You forgot a comma before your type choice.");
            Consume(A, "You forgot an 'a' before your type selection.");
            VBType type = Type();
            string varName = ((IdentToken)nameToken).Name;
            return new VarDecNode(type, varName, null);
        }
        throw new Exception();
    }

    private INode VarSet()
    {
        if (!Match(out var nameToken, typeof(IdentToken))) throw new Exception();
        Consume(To, "You missed a word: 'to'.");
        string varName = ((IdentToken)nameToken).Name;
        var value = Expression();
        return new VarSetNode(varName, value);
    }

    private INode ProcCall()
    {
        if (!Match(out var procNameToken, typeof(IdentToken))) throw new Exception();
        string procName = ((IdentToken)procNameToken).Name;
        List<IExpressionNode> args = [];
        int i = 0;
        bool shouldBeOnLast = false;
        while (true)
        {
            if (!IsAtEnd() &&
                (Check(Plus, Minus) || // Unary operators
                 Check(typeof(NumberToken), typeof(StringToken), typeof(IdentToken)) // Literals
                 )
               )
            {
                if (shouldBeOnLast) throw new Exception($"You used the word 'and' before the last thing you gave me when using '{procName}'.");
                args.Add(Expression());
                if (i > 0)
                {
                    Consume(Comma, $"You missed a comma between things you told me when using '{procName}'.");
                    if (Match(And)) shouldBeOnLast = true;   
                }
            }
            else
            {
                break;
            }
            ++i;
        }

        return new ProcCallNode(procName, args);
    }

    private INode IfStmt()
    {
        IExpressionNode cond = Expression();
        Consume(Then, "You missed a word: 'then'.");
        List<INode> statements = new List<INode>();
        bool hasElse = false;
        while (!Match(End))
        {
            statements.Add(Statement());
            if (Match(Otherwise))
            {
                hasElse = true;
                break;
            }
        }

        if (hasElse)
        {
            if (Match(Comma))
            {
                Consume(If, "You missed a word: 'if'.");
                return new IfNode(cond, statements, [IfStmt()]);
            }
            else
            {
                List<INode> elseStatements = new List<INode>();
                while (!Match(End))
                {
                    elseStatements.Add(Statement());
                }
                return new IfNode(cond, statements, elseStatements);
            }
        }
        return new IfNode(cond, statements);
    }

    private IExpressionNode Expression()
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
        
        if (Match(LParen))
        {
            IExpressionNode expression = Logical();
            if (!Match(RParen)) throw new Exception();
            return expression;
        }
        
        throw new Exception();
    }
}

public interface INode
{
    public T Accept<T>(IVisitor<T> visitor);
}

public class ProcCallNode(string name, List<IExpressionNode> args) : INode
{
    public string Name = name;
    public List<IExpressionNode> Args = args;

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitProcCallNode(this);
    }
}

public interface IExpressionNode : INode {}

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

    public IfNode(IExpressionNode cond, List<INode> then)
    {
        Condition = cond;
        Then = then;
    }

    public IfNode(IExpressionNode cond, List<INode> then, List<INode> otherwise)
    {
        Condition = cond;
        Then = then;
        Else = otherwise;
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitIfNode(this);
    }
}

public class VarDecNode(VBType type, string name, IExpressionNode? value) : INode
{
    public string Name = name;
    public VBType Type = type;
    public IExpressionNode? Value = value;
    
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVarDecNode(this);
    }
}

public class VarSetNode(string name, IExpressionNode value) : INode
{
    public string Name = name;
    public IExpressionNode Value = value;
    
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVarSetNode(this);
    }
}

public class VarRefNode(string name) : IExpressionNode
{
    public string Name = name;
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVarRefNode(this);
    }
}

// TODO: Implement these in parser, tree-walker, etc.

// public class CalculateNode : INode
// {
//     public IExpressionNode Expr;
// }
//
// public class RepeatLoopNode : INode
// {
//     public IExpressionNode Times;
//     public List<INode> Loop;
// }
//
// public class WhileLoopNode : INode
// {
//     public IExpressionNode Condition;
//     public List<INode> Loop;
// }