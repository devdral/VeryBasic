using System.Text;
using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class ByteCode
{
    private byte[] _program;

    public ByteCode(byte[] program)
    {
        _program = program;
    }
    
    public byte this[int index] => _program[index];

    public OpCode GetOpCodeAt(int index)
    {
        return (OpCode)this[index];
    }

    public override string ToString()
    {
        var s = new StringBuilder();
        foreach (var by in _program)
        {
            try
            {
                var opcode = (OpCode)by;
                s.Append(opcode.ToString());
            }
            catch (InvalidCastException)
            {
                s.Append("[data]");   
            }
            s.Append(' ');
        }

        return s.ToString();
    }

    public int Length => _program.Length;
}

public enum OpCode
{
    Jump,
    JumpIf,
    JumpTo,
    
    Push,
    Pop,
    
    Add,
    Sub,
    Mul,
    Div,
    
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    Equal,
    NotEqual,
    
    Load,
    Store,
    
    Invert,
    Negate,
    
    Call,
    Return,
    CallExtern
}