using System.Diagnostics.CodeAnalysis;
using System.Text;
using VeryBasic.Runtime.Executing.Errors;
using static VeryBasic.Runtime.Parsing.SyntaxTokenType;

namespace VeryBasic.Runtime.Parsing;

public class Parser
{
    private string _code;

    private int _index;

    private List<IToken> _tokens = [];
    private HashSet<string> _availableVars = new();
    private HashSet<string> _availableProcs = new();

    public Parser(string code)
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
        if (!Match(token)) throw new ParseException(errorMsg);
    }

    private bool Match(params SyntaxTokenType[] matches)
    {
        if (IsAtEnd()) return false;
        foreach (SyntaxTokenType match in matches)
        {
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
        foreach (SyntaxTokenType match in matches)
        {
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

    private bool Check(int offset, params SyntaxTokenType[] matches)
    {
        if (IsAtEnd()) return false;
        foreach (SyntaxTokenType match in matches)
        {
            if (Peek(offset) is SyntaxToken token)
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

    private IToken? Peek(int offset)
    {
        if (_index + offset >= _tokens.Count) return null;
        return _tokens[_index + offset];
    }

    // Node parsers

    private List<INode> Program()
    {
        List<INode> statements = new List<INode>();
        while (!IsAtEnd())
        {
            statements.Add(Statement());
            Consume(Period, "You forgot a period. Mind your punctuation!");
        }

        return statements;
    }

    private string VarName()
    {
        var name = new StringBuilder();
        while (true)
        {
            // Affix a space *before* every segment except the first.
            var st = name.ToString();
            if (_availableVars.Contains(st))
                return st;
            if (name.Length > 0)
                name.Append(' ');
            if (Match(out var part, typeof(IdentToken)))
            {
                name.Append(((IdentToken)part).Name);
            }
            else if (Match(out var reservedPart, typeof(SyntaxToken)))
            {
                var type = ((SyntaxToken)reservedPart).Type;
                if (type is Period)
                    throw new ParseException($"You never recorded anything named '{name.ToString()}'.");
                name.Append(type.ToString().ToLower());
            }
        }
    }

    private INode Statement()
    {
        if (Match(Declare))
        {
            return VarDec();
        }

        if (Match(Update))
        {
            if (Match(Item))
            {
                return ListSetStmt();
            }

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

        if (Match(While))
        {
            return WhileLoop();
        }

        if (Match(Repeat))
        {
            return RepeatLoop();
        }

        if (Match(Get))
        {
            return ListGetStmt();
        }

        if (Match(How))
        {
            Consume(To, "You missed the word 'to'.");
            return ProcDef();
        }

        if (Match(SyntaxTokenType.Convert))
        {
            return ConvertStmt();
        }

        if (Match(Return))
        {
            var expr = Expression();
            return new ReturnNode(expr);
        }

        throw new ParseException("I don't understand what you want me to do.");
    }

    private INode ConvertStmt()
    {
        var expr = Expression();
        Consume(To, "You missed the word 'to'.");
        Consume(A, "You missed the word 'a'.");
        var type = Type();
        return new ConvertNode(expr, type);
    }

    private INode ProcDef()
    {
        if (!Match(out var procName, typeof(StringToken)))
            throw new Exception("You forgot to put the name of what it was you're explaining how to do.");
        
        List<VBType> expectedArgs = new List<VBType>();
        List<string> args = new List<string>();
        if (Match(Given))
        {
            while (Match(out var argName, typeof(IdentToken)))
            {
                args.Add(((IdentToken)argName).Name);
                Consume(Comma, "You forgot a comma before your type choice.");
                Consume(A, "You forgot a word: 'a'.");
                expectedArgs.Add(Type());
                 if (!Match(Comma)) break;
                if (Match(And)) break;
            }
            
            if (!Match(Period) && Match(out var finalArgName, typeof(IdentToken)))
            {
                args.Add(((IdentToken)finalArgName).Name);
                Consume(Comma, "You forgot a comma before your type choice.");
                Consume(A, "You forgot a word: 'a'.");
                expectedArgs.Add(Type());
            }
        }
        
        VBType returnType = VBType.Void;
        if (Match(Return)) returnType = Type();
        List<INode> statements = new List<INode>();
        while (!(IsAtEnd() || Match(End)))
        {
            statements.Add(Statement());
            Consume(Period, "You missed a period. Mind your punctuation!.");
        }
        return new ProcDefNode(((StringToken)procName).String, expectedArgs, args, statements, returnType);
    }

    private string VarIdent()
    {
        string varName = "";
        int index = 0;
        while (Match(out IToken? tok, typeof(IdentToken)))
        {
            if (index > 0) varName += " ";
            varName += ((IdentToken)tok).Name;
            ++index;
        }
        return varName;
    }

    private INode ListSetStmt()
    {
        Consume(NumberSign, "You forgot a '#' (number sign).");
        var index = Expression();
        Consume(Of, "You missed a word: 'of'.");
        var list = Expression();
        Consume(To, "You missed a word: 'to'.");
        var value = Expression();
        return new ListSetNode(index, list, value);
    }

    private INode VarDec()
    {
        Consume(Variable, "You missed a word: 'variable'.");
        string varName = VarIdent();
        Consume(Comma, "You forgot a comma before your type choice.");
        Consume(A, "You forgot an 'a' before your type selection.");
        VBType type = Type();
        IExpressionNode? value = null;
        if (Match(Comma))
        {
            Consume(From, "You forgot a word: 'from'.");
            value = Expression();
        }
        return new VarDecNode(type, varName, value);
    }

    private INode VarSet()
    {
        string varName = VarIdent();
        Consume(To, "You missed a word: 'to'.");
        var value = Expression();
        return new VarSetNode(varName, value);
    }

    private INode ProcCall()
    {
        // TODO: Remove unnecessary allocations; use StringBuilder
        string procName = "";
        int index1 = 0;
        while (!(IsAtEnd() || Check(LBracket)))
        {
            string word;
            if (Match(out var procNameToken, typeof(IdentToken)))
            {
                word = ((IdentToken)procNameToken).Name;
            } 
            else if (Check(Period))
            {
                return new ProcCallNode(procName.Trim(), []);
            }
            else
                break;
            if (index1 > 0) procName += " ";
            procName += word;
            ++index1;
        }
        List<IExpressionNode> args = [];
        int index2 = 0;
        bool shouldBeOnLast = false;
        while (true)
        {
            if (!IsAtEnd() &&
                (Check(Plus, Minus, The, LBracket, A) || // Unary operators / keywords
                 Check(typeof(NumberToken), typeof(StringToken)) // Literals/varrefs
                 )
               )
            {
                args.Add(Expression());
                if (Check(Period)) break;
                Consume(Comma, $"You missed a comma between things you told me when using '{procName}'.");
                if (shouldBeOnLast) throw new Exception($"You used the word 'and' before the last thing you gave me when using '{procName}'.");
                if (Match(And)) shouldBeOnLast = true;
            }
            else
            {
                break;
            }
            ++index2;
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
            Consume(Period, "You missed a period. Mind your punctuation!");
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
                    Consume(Period, "You missed a period. Mind your punctuation!");
                }
                return new IfNode(cond, statements, elseStatements);
            }
        }
        return new IfNode(cond, statements);
    }

    private INode WhileLoop()
    {
        IExpressionNode cond = Expression();
        List<INode> statements = new List<INode>();
        while (!Match(End))
        {
            statements.Add(Statement());
            Consume(Period, "You missed a period. Mind your punctuation!");
        }

        return new WhileLoopNode(cond, statements);
    }

    private INode RepeatLoop()
    {
        IExpressionNode times = Expression();
        Consume(Times, "You missed a word: 'times'.");
        List<INode> statements = new List<INode>();
        while (!Match(End))
        {
            statements.Add(Statement());
            Consume(Period, "You missed a period. Mind your punctuation!");
        }
        return new RepeatLoopNode(times, statements);
    }

    private INode ListGetStmt()
    {
        Consume(Item, "You missed a word: 'item'.");
        Consume(NumberSign, "You missed a '#' (number sign).");
        var index = Expression();
        Consume(Of,  "You missed a word: 'of'.");
        var list = Expression();
        return new ListGetNode(index, list);
    }

    private IExpressionNode Expression()
    {
        return Term();
    }

    private IExpressionNode Term()
    {
        var expr = Product();

        if (Match(Plus))
        {
            expr = new BinaryOpNode(Term(), BinOp.Add, expr);
        } else if (Match(Minus))
        {
            expr = new BinaryOpNode(Term(), BinOp.Sub, expr);
        }

        return expr;
    }

    private IExpressionNode Product()
    {
        var expr = Unary();
        
        if (Match(Multiply))
        {
            expr = new BinaryOpNode(Term(), BinOp.Mul, expr);
        } else if (Match(Divide))
        {
            expr = new BinaryOpNode(Term(), BinOp.Div, expr);
        }

        return expr;
    }

    private IExpressionNode Unary()
    {
        var expr = Primary();
        if (Match(Not))
        {
            expr = new UnaryOpNode(UnaryOp.Invert, expr);
        }
        else if (Match(Minus))
        {
            expr = new UnaryOpNode(UnaryOp.Invert, expr);
        }

        return expr;
    }

    private IExpressionNode Primary()
    {
        if (Match(Yes))
        {
            return new ValueNode(new Value(true));
        }
        
        if (Match(No))
        {
            return new ValueNode(new Value(false));
        }

        if (Match(out var num, typeof(NumberToken)))
        {
            return new ValueNode(new Value(((NumberToken)num).Number));
        }

        if (Match(out var str, typeof(StringToken)))
        {
            return new ValueNode(new Value(((StringToken)str).String));
        }

        if (Check(typeof(IdentToken)))
        {
            return new VarRefNode(VarName());
        }

        var token = Peek();
        string term;
        if (token is SyntaxToken syntaxToken)
        {
            term = syntaxToken.Type.ToString().ToLower();
        }
        else if (token is IdentToken identToken)
        {
            term = identToken.Name;
        }
        else
        {
            throw new FatalException("Error displaying error?!");
        }

        throw new ParseException(
            $"I tried to understand—based on context—{term} as a thing, but that doesn't make sense.");
    }
}

public class ProcDefNode(string name, List<VBType> expectedArgs, List<string> args, List<INode> body, VBType returnType)
    : INode
{
    public string Name = name;
    public List<VBType> ExpectedArgs = expectedArgs;
    public List<string> Args = args;
    public List<INode> Body = body;
    public VBType ReturnType = returnType;

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitProcDefNode(this);
    }
}

public class ListGetNode(IExpressionNode index, IExpressionNode list) : INode
{
    public IExpressionNode Index = index;
    public IExpressionNode List = list;

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitListGetNode(this);
    }
}

public class ListSetNode(IExpressionNode index, IExpressionNode list, IExpressionNode value) : INode
{
    public IExpressionNode Index = index;
    public IExpressionNode List = list;
    public IExpressionNode Value = value;

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitListSetNode(this);
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

public class ListNode(List<IExpressionNode> items) : IExpressionNode
{
    public List<IExpressionNode> Items = items;

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitListNode(this);
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

public class VarDecNode(string name, IExpressionNode? value) : INode
{
    public string Name = name;
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

public class RepeatLoopNode : INode
{
    public IExpressionNode Times;
    public List<INode> Loop;

    public RepeatLoopNode(IExpressionNode times, List<INode> loop)
    {
        Times = times;
        Loop = loop;
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitRepeatLoopNode(this);
    }
}

public class WhileLoopNode : INode
{
    public IExpressionNode Condition;
    public List<INode> Loop;

    public WhileLoopNode(IExpressionNode condition, List<INode> loop)
    {
        Condition = condition;
        Loop = loop;
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitWhileLoopNode(this);
    }
}

public class ConvertNode(IExpressionNode expr, VBType target) : INode
{
    public IExpressionNode Expr = expr;
    public VBType Target = target;

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitConvertNode(this);
    }
}

public class ReturnNode(IExpressionNode value) : INode
{
    public IExpressionNode Value = value;
    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitReturnNode(this);
    }
}