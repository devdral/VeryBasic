using VeryBasic.Runtime.Executing.Errors;

namespace VeryBasic.Runtime.Executing;

public class VirtualMachine
{
    private ByteCode _program;
    private Stack<Value> _stack = new();
    private int _ip;
    private Value[] _locals = new Value[255];

    public VirtualMachine(ByteCode program)
    {
        _program = program;
    }

    public void Run()
    {
        while (_ip < _program.Length)
        {
            OpCode opCode = _program.GetOpCodeAt(_ip);
            Handle(opCode);
            ++_ip;
        }
    }

    private void Handle(OpCode opCode)
    {
        switch (opCode)
        {
            case OpCode.Jump:
                int newIp = Arg();
                _ip = newIp;
                break;
            case OpCode.JumpIf:
                int newIpIf = Arg();
                Value val = _stack.Pop();
                bool cond = val.Get<bool>();
                if (cond) _ip = newIpIf;
                break;
            case OpCode.JumpTo:
                _ip = _stack.Pop().Get<int>();
                break;
            
            case OpCode.Push:
                _stack.Push(LoadValue());
                break;
            case OpCode.Pop:
                _stack.Pop();
                break;

            case OpCode.Add:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to ADD not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to ADD not Number.");
                _stack.Push(new Value(top.Get<double>() + bottom.Get<double>()));                    
            }
                break;
            case OpCode.Sub:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to SUB not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to SUB not Number.");
                _stack.Push(new Value(top.Get<double>() - bottom.Get<double>()));
            }
                break;
            case OpCode.Mul:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to MUL not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to MUL not Number.");
                _stack.Push(new Value(top.Get<double>() * bottom.Get<double>()));
            }
                break;
            case OpCode.Div:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to DIV not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to DIV not Number.");
                _stack.Push(new Value(top.Get<double>() / bottom.Get<double>()));
            }
                break;
            
            case OpCode.Less:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to LESS not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to LESS not Number.");
                _stack.Push(new Value(top.Get<double>() < bottom.Get<double>()));
            }
                break;
            case OpCode.Greater:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to GREATER not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to GREATER not Number.");
                _stack.Push(new Value(top.Get<double>() > bottom.Get<double>()));
            }
                break;
            case OpCode.LessEqual:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to LEQ not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to LEQ not Number.");
                _stack.Push(new Value(top.Get<double>() <= bottom.Get<double>()));
            }
                break;
            case OpCode.GreaterEqual:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to GEQ not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to GEQ not Number.");
                _stack.Push(new Value(top.Get<double>() <= bottom.Get<double>()));
            }
                break;
            case OpCode.Equal:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to EQUAL not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to GEQ not Number.");
                _stack.Push(new Value(top.Get<double>() == bottom.Get<double>()));
            }
                break;
            case OpCode.NotEqual:
            {
                var top = _stack.Pop();
                if (top.Type != VBType.Number) throw new FatalException("A arg to NEQ not Number.");
                var bottom = _stack.Pop();
                if (bottom.Type != VBType.Number) throw new FatalException("B arg to NEQ not Number.");
                _stack.Push(new Value(top.Get<double>() != bottom.Get<double>()));
            }
                break;

            case OpCode.Load:
            {
                byte var = Arg();
                Value value = _locals[var];
                _stack.Push(value);
            }
                break;
            case OpCode.Store:
            {
                byte var = Arg();
                _locals[var] = _stack.Pop();
            }
                break;
        }
    }

    private byte Arg()
    {
        return _program[_ip++];
    }

    private Value LoadValue()
    {
        VBType type = (VBType)Arg();
        if (type == VBType.Boolean)
        {
            _stack.Push(new Value(Arg()==1));
        }
        else if (type == VBType.Number)
        {
            byte[] bytes = [Arg(), Arg(), Arg(), Arg()];
            try
            {
                return new Value(BitConverter.ToDouble(bytes, 0));
            }
            catch
            {
                throw new FatalException("Could not decode double!");
            }
        }
        else if (type == VBType.String)
        {
            byte[] lengthBytes = [Arg(), Arg()];
            short length = BitConverter.ToInt16(lengthBytes, 0);
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                byte[] bytes = [Arg(), Arg()];
                chars[i] = BitConverter.ToChar(bytes, 0);
            }
            return new Value(new string(chars));
        }
        else if (type == VBType.List)
        {
            byte[] lengthBytes = [Arg(), Arg()];
            short length = BitConverter.ToInt16(lengthBytes, 0);
            Value[] values = new Value[length];
            for (int i = 0; i < length; i++)
            {
                values[i] = LoadValue();
            }
            return new Value(values.ToList());
        }
        
        throw new FatalException("Cannot load void!");
    }
}