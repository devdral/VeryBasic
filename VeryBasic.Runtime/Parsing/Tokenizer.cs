namespace VeryBasic.Runtime.Parsing;

public class Tokenizer
{
    private string _source;
    private int _index = 0;
    private List<IToken> _tokens = new List<IToken>();

    public Tokenizer(string source)
    {
        _source = source;
    }

    public List<IToken> Tokenize()
    {
        void HandleString()
        {
            char c = Advance();
            string str = "";
            while (!IsAtEnd() && c != '"')
            {
                str += c;
                c = Advance();
            }
            _tokens.Add(new StringToken(str));
        }

        void HandleNumber(char c)
        {
            string str = c.ToString();
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                str += Advance();
            }
            _tokens.Add(new NumberToken(double.Parse(str)));
        }

        bool IsSyntaxTokenPart(char c)
        {
            return (char.IsLetter(c) ||
                    c
                    is '_'
                    or '='
                    or '!'
                    or '<'
                    or '>'
                    or '+'
                    or '-'
                    or '*'
                    or '/'
                );
        }
        
        char c;
        while (!IsAtEnd())
        {
            c = Advance();
            if (char.IsDigit(c))
            {
                HandleNumber(c);
            } else if (c == '"') {
                HandleString();
            } else if (IsSyntaxTokenPart(c))
            {
                string token = c.ToString();
                while (!IsAtEnd() && IsSyntaxTokenPart(Peek()))
                {
                    token += Advance();
                }
                switch (token.ToLower())
                {
                    case "done":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.End));
                        break;
                    case "if":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.If));
                        break;
                    case "else":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Else));
                        break;
                    case "repeat":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Repeat));
                        break;
                    case "create":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Declare));
                        break;
                    case "variable":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Variable));
                        break;
                    case "change":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Update));
                        break;
                    case "to":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.To));
                        break;
                    case "the":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.The));
                        break;
                    case "while":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.While));
                        break;
                    case "times":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Times));
                        break;
                    case "otherwise":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Otherwise));
                        break;
                    case ",":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Comma));
                        break;
                    case "a":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.A));
                        break;
                    case "then":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Then));
                        break;
                    case "constant":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Constant));
                        break;
                    case "from":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.From));
                        break;
                    case "yes":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Yes));
                        break;
                    case "no":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.No));
                        break;
                    case "+":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Plus));
                        break;
                    case "-":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Minus));
                        break;
                    case "*":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Multiply));
                        break;
                    case "/":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Divide));
                        break;
                    case "<":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.LessThan));
                        break;
                    case ">":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.GreaterThan));
                        break;
                    case "<=":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.LEq));
                        break;
                    case ">=":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.GEq));
                        break;
                    case "=":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Equal));
                        break;
                    case "=/=":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.NotEqual));
                        break;
                    case "and":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.And));
                        break;
                    case "or":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Or));
                        break;
                    case "not":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Not));
                        break;
                    case "calculate":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Calculate));
                        break;
                    case "result":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Result));
                        break;
                    default:
                        _tokens.Add(new IdentToken(token));
                        break;
                }
            }
        }
        return _tokens;
    }

    private char Advance()
    {
        char c = _source[_index];
        _index++;
        return c;
    }

    private bool IsAtEnd()
    {
        return _index >= _source.Length;
    }

    private char Peek()
    {
        return _source[_index];
    }
}

public interface IToken {}

public class SyntaxToken(SyntaxTokenType type) : IToken
{
    public SyntaxTokenType Type { get; } = type;
    
    public override string ToString() => Type.ToString();
}
public enum SyntaxTokenType
{
    End,
    Declare,
    Variable,
    Update,
    If,
    Else,
    Repeat,
    To,
    The,
    While,
    Times,
    Otherwise,
    Comma,
    A,
    Then,
    Constant,
    From,
    LessThan,
    GreaterThan,
    GEq,
    LEq,
    Equal,
    NotEqual,
    Not,
    And,
    Or,
    Calculate,
    Result,
    Divide,
    Multiply,
    Plus,
    Minus,
    Yes,
    No,
}

public class NumberToken(double number) : IToken
{
    public double Number = number;
    public override string ToString() => Number.ToString();
}

public class StringToken(string s) : IToken
{
    public string String = s;
    public override string ToString() => String;
}

public class IdentToken(string ident) : IToken
{
    public string Name = ident;
    public override string ToString() => $"ident {Name}";
}