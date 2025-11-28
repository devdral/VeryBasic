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
            string str = "";
            while (!IsAtEnd() && Peek() != '"')
            {
                str += Advance();
            }

            Advance();
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
            }
            else if (c is '"')
            {
                HandleString();
            }
            else if (c is '(') 
            {
                _tokens.Add(new SyntaxToken(SyntaxTokenType.LParen));
            }
            else if (c is ')') 
            {
                _tokens.Add(new SyntaxToken(SyntaxTokenType.RParen));
            }
            else if (c is '#')
            {
                _tokens.Add(new SyntaxToken(SyntaxTokenType.NumberSign));
            }
            else if (IsSyntaxTokenPart(c))
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
                    case "record":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Save));
                        break;
                    case "get":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Get));
                        break;
                    case "item":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Item));
                        break;
                    case "in":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Of));
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
                    case "is":
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
                    case "result":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Result));
                        break;
                    case "list":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.List));
                        break;
                    case "how":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.How));
                        break;
                    case "given":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Given));
                        break;
                    case "return":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Return));
                        break;
                    case "convert":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.Convert));
                        break;
                    case "as":
                        _tokens.Add(new SyntaxToken(SyntaxTokenType.As));
                        break;
                    default:
                        _tokens.Add(new IdentToken(token.ToLower()));
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
    
    private char? Peek(int offset)
    {
        if (_index + offset >= _source.Length) return null;
        return _source[_index + offset];
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
    Save,
    List,
    Update,
    If,
    Else,
    Repeat,
    To,
    The,
    While,
    Times,
    Otherwise,
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
    Result,
    Divide,
    Multiply,
    Plus,
    Minus,
    Yes,
    No,
    LParen,
    RParen,
    Get,
    Item,
    Of,
    NumberSign,
    How,
    Given,
    Return,
    Convert,
    As,
    Number,
    Boolean,
    String
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