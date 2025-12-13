using System.Diagnostics.CodeAnalysis;
using System.Text;
using VeryBasic.Runtime.Executing.Errors;
using static VeryBasic.Runtime.Parsing.SyntaxTokenType;

namespace VeryBasic.Runtime.Parsing;

public class Parser
{
    public string Code
    {
        get => _code;
        set
        {
            Reset();
            _code = value;   
        }
    }

    private int _index;

    private List<IToken> _tokens = [];
    private string _code;
    private HashSet<string> _availableVars = new();
    private Dictionary<string, List<string>> _availableProcs = new();
    private HashSet<string> _availableParams = new();

    public Parser(string code)
    {
        Code = code;
    }

    public List<INode> Parse()
    {
        List<IToken> tokens = new Tokenizer(Code).Tokenize();
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

    public void Reset()
    {
        _index = 0;
    }

    // Node parsers

    private List<INode> Program()
    {
        List<INode> statements = new List<INode>();
        while (!IsAtEnd())
        {
            var stmt = Statement();
            if (stmt is not null)
                statements.Add(stmt);
            else
                break;
        }

        return statements;
    }

    private string VarName()
    {
        var name = new StringBuilder();
        var restore = _index;
        while (!IsAtEnd())
        {
            var tok = Advance();
            if (tok is IdentToken ident)
            {
                if (name.Length > 0)
                    name.Append(' ');
                name.Append(ident.Name);
            }
            else if (tok is SyntaxToken syntaxToken)
            {
                if (name.Length > 0)
                    name.Append(' ');
                name.Append(syntaxToken.Type.ToString().ToLower());
            }
            else
                break;
            
            var nameStr = name.ToString();
            if (_availableVars.Contains(nameStr) || _availableParams.Contains(nameStr))
                return nameStr;
        }

        if (_availableVars.Contains(name.ToString()))
            return name.ToString();
        _index = restore;
        throw new ParseException($"I have no record of '{name}'.");
    }

    private VBType Type()
    {
        if (Match(Number))
        {
            return VBType.Number;
        } 
        else if (Match(SyntaxTokenType.Boolean))
        {
            return VBType.Boolean;
        }
        else if (Match(SyntaxTokenType.String))
        {
            return VBType.String;
        }

        throw new ParseException("Here was supposed to be the type of thing you wanted: number, string, or boolean.");
    }

    private INode? Statement()
    {
        if (Match(Save))
        {
            return VarDec();
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

        if (Match(To))
        {
            return ProcDef();
        }

        if (Match(SyntaxTokenType.Convert))
        {
            return ConvertStmt();
        }

        if (Match(Return))
        {
            return ReturnStmt();
        }

        if (Match(Update))
        {
            return UpdateStmt();
        }

        return ProcCall();
    }

    private VarSetNode UpdateStmt()
    {
        var name = VarName();
        Consume(To, "You missed a word: 'to'.");
        var expr = Expression();
        return new VarSetNode(name, expr);
    }

    private VarDecNode VarDec()
    {
        var expr = Expression();
        Consume(As, "You missed a word: 'as'.");

        if (!Match(out var nameTok, typeof(StringToken)))
            throw new ParseException("You were supposed to put the name of the record in quotes.");
        
        var name = ((StringToken)nameTok).String;
        _availableVars.Add(name);
        return new VarDecNode(name, expr);
    }

    private IfNode IfStmt()
    {
        var cond = Expression();
        Consume(Then, "You missed a word: then.");
        var stmts = new List<INode>();
        while (!Check(End))
        {
            stmts.Add(Statement());
        }

        if (Match(Otherwise))
        {
            List<INode> otherwise;
            if (Match(If))
            {
                otherwise = [IfStmt()];
            }
            else
            {
                otherwise = [];
                while (!Match(End))
                {
                    otherwise.Add(Statement());
                }
            }

            return new IfNode(cond, stmts, otherwise);
        }

        return new IfNode(cond, stmts);
    }

    private WhileLoopNode WhileLoop()
    {
        var cond = Expression();
        var stmts = new List<INode>();
        while (!Match(End))
        {
            stmts.Add(Statement());
        }

        return new WhileLoopNode(cond, stmts);
    }

    private RepeatLoopNode RepeatLoop()
    {
        var times = Expression();
        Consume(Times, "You missed a word: 'times'.");
        var stmts = new List<INode>();
        while (!Match(End))
        {
            stmts.Add(Statement());
        }

        return new RepeatLoopNode(times, stmts);
    }

    private ProcCallNode? ProcCall()
    {
        var name = ProcName();
        if (name is null)
            return null;
        var keywords = _availableProcs[name];
        var index = 0;
        var args = new List<IExpressionNode>();
        while (true)
        {
            var restore = _index;
            try
            {
                args.Add(Expression());
            }
            catch (ParseException)
            {
                _index = restore;
                break;
            }

            if (index >= keywords.Count)
                break;

            var target = keywords[index];
            var current = new StringBuilder();
            while (target != current.ToString())
            {
                if (current.Length > 0)
                    current.Append(' ');
                if (Match(out var syntaxToken, typeof(SyntaxToken)))
                {
                    current.Append(((SyntaxToken)syntaxToken).Type.ToString().ToLower());
                } else if (Match(out var identToken, typeof(IdentToken)))
                {
                    current.Append(((IdentToken)identToken).Name);
                }
                else
                    throw new ParseException($"You were supposed to put '{target}', but you put '{current}'.");
            }

            index++;
        }

        return new ProcCallNode(name, args);
    }

    private ProcDefNode ProcDef()
    {
        if (!Match(out var nameTok, typeof(StringToken)))
            throw new ParseException("Put the name of what you wanted me to teach me in quotes after 'how to'.");
        var name = ((StringToken)nameTok).String.ToLower();
        var args = new List<string>();
        var keywords = new List<string>();
        if (Match(Given))
        {
            var keyword = new StringBuilder();
            var isFirst = true;
            while (!Match(Do))
            {
                if (Match(out var stringToken, typeof(StringToken)))
                {
                    if (!isFirst)
                    {
                        keywords.Add(keyword.ToString());
                        keyword.Clear();   
                    }
                    var arg = ((StringToken)stringToken).String;
                    args.Add(arg);
                    _availableParams.Add(arg);
                }
                else if (Match(out var syntaxToken, typeof(SyntaxToken)))
                {
                    var tok = (SyntaxToken)syntaxToken;
                    keyword.Append(tok.ToString().ToLower());
                }
                else if (Match(out var identToken, typeof(IdentToken)))
                {
                    var tok = (IdentToken)identToken;
                    keyword.Append(tok.Name.ToLower());
                }

                isFirst = false;
            }

            var restore = _index;
            if (!Match(out var stringToken2, typeof(StringToken)))
                _index = restore;
            else
            {
                var arg = ((StringToken)stringToken2).String;
                args.Add(arg);
                _availableParams.Add(arg);
            }
        }

        var stmts = new List<INode>();
        while (!Match(End))
        {
            stmts.Add(Statement());
        }

        _availableProcs.Add(name, keywords);
        _availableParams.Clear();
        return new ProcDefNode(name, args, stmts);
    }

    private string? ProcName()
    {
        var name = new StringBuilder();
        var restore = _index;
        while (!IsAtEnd())
        {
            var tok = Advance();
            if (tok is IdentToken ident)
            {
                if (name.Length > 0)
                    name.Append(' ');
                name.Append(ident.Name);
            }
            else if (tok is SyntaxToken syntaxToken)
            {
                if (name.Length > 0)
                    name.Append(' ');
                name.Append(syntaxToken.Type.ToString().ToLower());
            }
            else
                break;
            var nameStr = name.ToString();
            if (_availableProcs.ContainsKey(nameStr))
                return nameStr;
        }

        _index = restore;
        if (name.ToString() is "")
            return null;
        throw new ParseException($"I don't know how to '{name}'.");
    }

    private ConvertNode ConvertStmt()
    {
        var expr = Expression();
        Consume(To, "After the thing you wanted to convert, put 'to'.");
        Consume(A, "Before the type of thing you wanted to make, put 'a'.");
        return new ConvertNode(expr, Type());
    }

    private ReturnNode ReturnStmt()
    {
        return new ReturnNode(Expression());
    }

    private IExpressionNode Expression()
    {
        return Boolean();
    }

    private IExpressionNode Boolean()
    {
        var expr = Comparison();

        while (true)
        {
            if (Match(And))
            {
                var right = Comparison();
                expr = new BinaryOpNode(expr, BinOp.And, right);
            }
            else if (Match(Or))
            {
                var right = Comparison();
                expr = new BinaryOpNode(expr, BinOp.Or, right);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private IExpressionNode Comparison()
    {
        var expr = Term();
        while (true)
        {
            if (Match(LessThan))
            {
                var right = Term();
                expr = new BinaryOpNode(expr, BinOp.LessThan, right);
            }
            else if (Match(GreaterThan))
            {
                var right = Term();
                expr = new BinaryOpNode(expr, BinOp.GreaterThan, right);
            }
            else if (Match(LEq))
            {
                var right = Term();
                expr = new BinaryOpNode(expr, BinOp.LEq, right);
            }
            else if (Match(GEq))
            {
                var right = Term();
                expr = new BinaryOpNode(expr, BinOp.GEq, right);
            }
            else if (Match(Equal))
            {
                var right = Term();
                expr = new BinaryOpNode(expr, BinOp.Equal, right);
            }
            else if (Match(NotEqual))
            {
                var right = Term();
                expr = new BinaryOpNode(expr, BinOp.NotEqual, right);
            }
            else
                break;
        }

        return expr;
    }
    private IExpressionNode Term()
    {
        var expr = Product();
        while (true)
        {
            if (Match(Plus))
            {
                var right = Product();
                expr = new BinaryOpNode(expr, BinOp.Add, right);
            } else if (Match(Minus))
            {
                var right = Product();
                expr = new BinaryOpNode(expr, BinOp.Sub, right);
            }
            else
                break;
        }

        return expr;
    }

    private IExpressionNode Product()
    {
        var expr = Unary();

        while (true)
        {
            if (Match(Multiply))
            {
                var right = Unary();
                expr = new BinaryOpNode(expr, BinOp.Mul, right);
            } else if (Match(Divide))
            {
                var right = Unary();
                expr = new BinaryOpNode(expr, BinOp.Div, right);
            }
            else
                break;
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
        if (Match(LParen))
        {
            var expr = Expression();
            Consume(RParen, "Make sure to include the right parenthesis.");
            return expr;
        }
        
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

        var restore = _index;
        if (Match(The))
        {
            if (!Match(Result))
                _index = restore;
            else
            {
                return new TheResultNode();
            }
        }
        
        return new VarRefNode(VarName());
    }

    public void RegisterPreexistingProcedure(string name, int arity)
    {
        if (_availableProcs.ContainsKey(name))
            return;
        _availableProcs.Add(name, Enumerable.Repeat(string.Empty, arity).ToList());
    }
}

public class ProcDefNode(string name, List<string> args, List<INode> body)
    : INode
{
    public string Name = name;
    public List<string> Args = args;
    public List<INode> Body = body;

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

public class VarDecNode(string name, IExpressionNode value) : INode
{
    public string Name = name;
    public IExpressionNode Value = value;
    
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