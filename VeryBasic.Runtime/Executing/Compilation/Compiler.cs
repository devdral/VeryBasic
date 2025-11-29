using System.Net.NetworkInformation;
using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class Compiler
{
    private List<INode>? _ast;
    private List<byte> _program = [];
    private Dictionary<string, Variable> _vars = new();
    private Dictionary<string, Procedure> _procedures = new();
    private Dictionary<string, ExternTable.Signature> _externProcedures = new();
    private byte _nextVar = 0;
    private Queue<byte> _freedVars = [];
    private int _scopeLevel = 0;
    private int _nextProcAddress = 0;
    private VBType _priorResult = VBType.Void;
    private string? _currentProc;
    private bool _returned;
    private VBType? _requestedType = null;
    
    private class Variable
    {
        public VBType Type;
        public byte Id;
        public int Scope;

        public Variable(VBType type, byte id, int scope)
        {
            Type = type;
            Id = id;
            Scope = scope;
        }
    }

    private class Procedure
    {
        public VBType ReturnType;
        public int Address;
        public OrderedDictionary<string, Param> Params = new();

        public Procedure(VBType returnType, int address)
        {
            ReturnType = returnType;
            Address = address;
        }
    }

    private class Param
    {
        public VBType Type;
        public int Pos;
        public byte Cell;

        public Param(VBType type, int pos, byte cell)
        {
            Type = type;
            Pos = pos;
            Cell = cell;
        }
    }
    
    public ByteCode Compile(Parser parser)
    {
        _ast = parser.Parse();
        _program = [];
        Operation (OpCode.Jump);
        // Backpatch later
        _nextProcAddress = 5;
        IncludeAddress(_nextProcAddress);
        foreach (var node in _ast)
        {
            _priorResult = ProcessNode(node);
        }

        return new ByteCode(_program.ToArray());
    }

    private byte NextVar()
    {
        if (_freedVars.TryDequeue(out var reused))
        {
            return reused;
        }
        return _nextVar++;
    }

    private void Operation(OpCode op)
    {
        if (_currentProc is not null)
        {
            if (_nextProcAddress > _program.Count)
                _program.Add((byte)op);
            else
                _program.Insert(_nextProcAddress, (byte)op);
            _nextProcAddress++;
            BackpatchAddress(1, _nextProcAddress);
        }
        else
            _program.Add((byte)op);
    }

    private void Arg(byte arg)
    {
        if (_currentProc is not null)
        {
            if (_nextProcAddress > _program.Count)
                _program.Add(arg);
            else
                _program.Insert(_nextProcAddress, arg);
            _nextProcAddress++;
            BackpatchAddress(1, _nextProcAddress);
        }
        else
            _program.Add(arg);
    }

    private void Arg(byte[] args)
    {
        foreach (var arg in args)
        {
            Arg(arg);
        }
    }

    private void IncludeValue(Value value)
    {
        Arg((byte)value.Type);
        switch (value.Type)
        {
            case VBType.Boolean:
                Arg(value.Get<bool>() ? (byte)0 : (byte)1);
                break;
            case VBType.Number:
                Arg(
                    BitConverter.GetBytes(value.Get<double>())
                );
                break;
            case VBType.String:
                var str = value.Get<string>();
                Arg(BitConverter.GetBytes((short)str.Length));
                foreach (char ch in str)
                {
                    Arg(BitConverter.GetBytes(ch));
                }
                break;
            case VBType.List:
                var list = value.Get<List<Value>>();
                Arg(BitConverter.GetBytes(list.Count));
                foreach (Value value2 in list)
                {
                    IncludeValue(value2);
                }
                break;
            default:
                throw new FatalException("Cannot include void!");
        }
    }

    private void IncludeAddress(int newAddress)
    {
        var bytes = BitConverter.GetBytes(newAddress);
        _program.AddRange(bytes);
    }

    private void BackpatchAddress(int modifiedAddress, int newAddress)
    {
        var bytes = BitConverter.GetBytes(newAddress);
        for (var index = 0; index < bytes.Length; index++)
        {
            var @byte = bytes[index];
            _program[modifiedAddress + index] = @byte;
        }
    }

    private int ReserveAddress()
    {
        var pos = _program.Count;
        IncludeAddress(0);
        return pos;
    }

    private void EnterScope()
    {
        _scopeLevel++;
    }

    private void LeaveScope()
    {
        _scopeLevel--;
        foreach (var var in _vars)
        {
            if (var.Value.Scope > _scopeLevel)
            {
                _vars.Remove(var.Key);
                _freedVars.Enqueue(var.Value.Id);
                Operation(OpCode.Free);
                Arg      (var.Value.Id);
            }
        }
    }
    
    private VBType ProcessNode(INode node)
    {
        switch (node)
        {
            case VarDecNode dec:
            {
                var id = NextVar();
                var type = ProcessNode(dec.Value);
                Operation(OpCode.Store);
                Arg(id);
                _vars[dec.Name] = new Variable(type, id, _scopeLevel);

                return VBType.Void;
            }
            case VarSetNode setVar:
            {
                var variable = _vars[setVar.Name];
                var actualType = ProcessNode(setVar.Value);
                if (variable.Type != actualType)
                    throw new ParseException($"A {actualType.ToString()} is not a {variable.Type.ToString()}.");
                Operation(OpCode.Store);
                Arg(variable.Id);
                return VBType.Void;
            }
            case ValueNode val:
            {
                Operation(OpCode.Push);
                IncludeValue(val.Value);
                return val.Value.Type;
            }
            case BinaryOpNode binOp:
            {
                return ProcessBinOp(binOp);
            }
            case UnaryOpNode unOp:
            {
                return ProcessUnaryOp(unOp);
            }
            case TheResultNode:
            {
                if (_priorResult == VBType.Void)
                    throw new ParseException("The prior statement didn't result in anything.");
                // Otherwise, it will be stored on the stack
                return _priorResult;
            }
            case VarRefNode varRef:
            {
                if (_currentProc is not null && _procedures[_currentProc].Params.TryGetValue(varRef.Name, out var param))
                {
                    Operation(OpCode.Load);
                    Arg      (param.Cell);
                    if (param.Type != _requestedType &&
                        !(param.Type is VBType.Unknown ||
                        _requestedType is VBType.Unknown))
                        throw new ParseException(
                            $"This would require that '{varRef.Name}' to be both a {param.Type} and a {_requestedType} at the same time.");
                    param.Type = _requestedType ?? VBType.Unknown;
                    return param.Type;
                }
                else
                {
                    if (!_vars.TryGetValue(varRef.Name, out var var))
                        throw new ParseException($"You never said what '{varRef.Name}' was.");
                    Operation(OpCode.Load);
                    Arg(var.Id);
                    return var.Type;
                }
            }
            case IfNode ifNode:
            {
                EnterScope();
                // For if's with no else's, jump over this branch if the condition is false.

                ProcessNode(ifNode.Condition);
                Operation(OpCode.Invert);

                Operation(OpCode.JumpIf);
                var firstJumpAddress = _program.Count;
                IncludeAddress(0);
                foreach (INode then in ifNode.Then)
                {
                    _priorResult = ProcessNode(then);
                }

                var thenLength = _program.Count;
                if (thenLength > 0)
                {
                    BackpatchAddress(firstJumpAddress, thenLength);
                }

                if (ifNode.Else is not null)
                {
                    Operation(OpCode.Jump);
                    IncludeAddress(0);
                    foreach (INode otherwise in ifNode.Else)
                    {
                        _priorResult = ProcessNode(otherwise);
                    }

                    var elseLength = _program.Count;
                    if (elseLength > 0)
                    {
                        BackpatchAddress(thenLength + 1, elseLength);
                    }
                }

                LeaveScope();
                return VBType.Void;
            }
            case WhileLoopNode whileNode:
            {
                EnterScope();
                var returnPos = _program.Count;
                ProcessNode(whileNode.Condition);
                Operation(OpCode.Invert);

                Operation(OpCode.JumpIf);
                var addrPos = _program.Count;
                IncludeAddress(0);

                foreach (INode node2 in whileNode.Loop)
                {
                    _priorResult = ProcessNode(node2);
                }

                Operation(OpCode.Jump);
                IncludeAddress(returnPos);
                var loopEnd = _program.Count;
                BackpatchAddress(addrPos, loopEnd);
                LeaveScope();
                return VBType.Void;
            }
            case RepeatLoopNode repeatLoopNode:
            {
                EnterScope();
                ProcessNode(repeatLoopNode.Times);
                Operation(OpCode.Dup);
                Operation(OpCode.Push);
                IncludeValue(new Value(0d));
                Operation(OpCode.LessEqual);
                Operation(OpCode.JumpIf);
                var skipAddr = ReserveAddress();

                var threshVar = NextVar();
                Operation(OpCode.Store);
                Arg(threshVar);
                Operation(OpCode.Push);
                IncludeValue(new Value(0d));
                var iterVar = NextVar();
                Operation(OpCode.Store);
                Arg(iterVar);
                var returnPos = _program.Count;

                foreach (var stmt in repeatLoopNode.Loop)
                {
                    ProcessNode(stmt);
                }

                Operation(OpCode.Load);
                Arg(iterVar);
                Operation(OpCode.Push);
                IncludeValue(new Value(1d));
                Operation(OpCode.Add);
                Operation(OpCode.Dup);
                Operation(OpCode.Store);
                Arg(iterVar);
                Operation(OpCode.Load);
                Arg(threshVar);
                Operation(OpCode.Less);
                Operation(OpCode.JumpIf);
                IncludeAddress(returnPos);
                BackpatchAddress(skipAddr, _program.Count);
                LeaveScope();
                return VBType.Void;
            }
            case ProcCallNode procCall:
            {
                var args = procCall.Args;
                if (!_procedures.TryGetValue(procCall.Name, out var proc))
                {
                    if (_externProcedures.TryGetValue(procCall.Name, out var externProc))
                    {
                        if (args.Count != externProc.Args.Count)
                            throw new ParseException(
                                $"You put too few (or too many) arguments to use '{procCall.Name}'.");
                        for (var i = 0; i < args.Count; i++)
                        {
                            var arg = ProcessNode(args[i]);
                            var expectedType = externProc.Args[i];
                            if (arg != expectedType)
                                throw new ParseException(
                                    $"You put a {arg.ToString()}, when you should have put a {expectedType.ToString()}.");
                        }

                        Operation(OpCode.CallExtern);
                        IncludeValue(new Value(procCall.Name));
                        return externProc.ReturnType;
                    }
                    else
                        throw new ParseException($"I don't know how to {procCall.Name}.");
                }

                if (args.Count != proc.Params.Count)
                    throw new ParseException($"You put too few (or too many) arguments to use '{procCall.Name}'.");
                for (var i = 0; i < args.Count; i++)
                {
                    var arg = ProcessNode(args[i]);
                    var expectedType = proc.Params.GetAt(i).Value.Type;
                    if (arg != expectedType)
                        throw new ParseException(
                            $"You put a {arg.ToString()}, when you should have put a {expectedType.ToString()}.");
                }

                Operation(OpCode.Call);
                IncludeAddress(proc.Address);
                return proc.ReturnType;
            }
            case ProcDefNode procDef:
            {
                if (_procedures.ContainsKey(procDef.Name))
                    throw new ParseException($"You already told me how to {procDef.Name}.");
                var address = _program.Count;
                var proc = new Procedure(VBType.Unknown, address);
                var cells = new List<byte>();
                for (var index = 0; index < procDef.Args.Count; index++)
                {
                    var name = procDef.Args[index];
                    var id = NextVar();
                    proc.Params[name] = new Param(VBType.Unknown, index, id);
                    cells.Add(id);
                }
                
                _procedures[procDef.Name] = proc;
                _currentProc = procDef.Name;

                for (var index = cells.Count-1; index >= 0; index--)
                {
                    var cell = cells[index];
                    Operation(OpCode.Store);
                    Arg      (cell);
                }

                _returned = false;
                foreach (var stmt in procDef.Body)
                {
                    _priorResult = ProcessNode(stmt);
                }

                if (!_returned)
                {
                    Operation(OpCode.Return);
                    proc.ReturnType = VBType.Void;
                }

                foreach (var param in proc.Params)
                {
                    if (param.Value.Type is VBType.Unknown)
                    {
                        throw new ParseException(
                            $"You didn't give me enough information to know what type '{param.Key}' is.");
                    }
                }

                _currentProc = null;
                return VBType.Void;
            }
            case ConvertNode conversion:
            {
                var target = conversion.Target;
                var source = ProcessNode(conversion.Expr);
                var isValid = source == target ||
                              (source == VBType.Number &&
                               target == VBType.String) ||
                              (source == VBType.Boolean &&
                               target == VBType.String) ||
                              (source == VBType.String &&
                               target == VBType.Number) ||
                              (source == VBType.String &&
                               target == VBType.Boolean);
                if (!isValid)
                    throw new ParseException($"I can't turn a {source} into a {target}.");
                Operation(OpCode.Convert);
                IncludeType(target);
                return conversion.Target;
            }
            case ReturnNode returnNode:
            {
                if (_currentProc is null)
                    throw new ParseException("I can't return something when I wasn't told to follow a procedure in the first place.");
                _returned = true;
                var type = ProcessNode(returnNode.Value);
                Operation(OpCode.Return);
                _procedures[_currentProc].ReturnType = type;
                return VBType.Void;
            }
    }
        throw new FatalException($"Unknown node type: {node.GetType()}");
    }

    private VBType ProcessBinOp(BinaryOpNode node)
    {
        switch (node.Op)
        {
            case BinOp.Add:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't add two things if they aren't numbers.");
                Operation(OpCode.Add);
                return VBType.Number;
            }
            case BinOp.Sub:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't subtract two things if they aren't numbers.");
                Operation(OpCode.Sub);
                return VBType.Number;
            }
            case BinOp.Mul:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't add two things if they aren't numbers.");
                Operation(OpCode.Mul);
                return VBType.Number;
            }
            case BinOp.Div:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't divide two things if they aren't numbers.");
                Operation(OpCode.Div);
                return VBType.Number;
            }
            case BinOp.Equal:
            {
                _requestedType = VBType.Unknown;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                Operation(OpCode.Equal);
                return VBType.Boolean;
            }
            case BinOp.NotEqual:
            {
                _requestedType = VBType.Unknown;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                Operation(OpCode.NotEqual);
                return VBType.Boolean;
            }
            case BinOp.GreaterThan:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.Greater);
                return VBType.Boolean;
            }
            case BinOp.LessThan:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.Less);
                return VBType.Boolean;
            }
            case BinOp.GEq:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.GreaterEqual);
                return VBType.Boolean;
            }
            case BinOp.LEq:
            {
                _requestedType = VBType.Number;
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.LessEqual);
                return VBType.Boolean;
            }
            default:
                throw new FatalException("Binary operation not implemented.");
        }
    }

    private VBType ProcessUnaryOp(UnaryOpNode node)
    {
        switch (node.Op)
        {
            case UnaryOp.Invert:
            {
                _requestedType = VBType.Boolean;
                var arg = ProcessNode(node.Expr);
                if (arg != VBType.Boolean)
                    throw new ParseException($"I can't know when a {arg.ToString()} is true or false; I can't know the opposite either.");
                Operation(OpCode.Invert);
                return VBType.Boolean;
            }
            case UnaryOp.Negate:
            {
                _requestedType = VBType.Number;
                var arg = ProcessNode(node.Expr);
                if (arg != VBType.Number)
                    throw new ParseException($"I can't find the inverse to a {arg.ToString()}; it must be a number.");
                Operation(OpCode.Negate);
                return VBType.Boolean;
            }
        }
        throw new FatalException("Invalid unary operator.");
    }

    private void IncludeType(VBType type)
    {
        Arg((byte)type);
    }

    public void RegisterExterns(ExternTable table)
    {
        foreach (var entry in table.Externs)
        {
            _externProcedures[entry.Key] = entry.Value.Signature;
        }
    }
}