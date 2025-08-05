using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class Compiler
{
    private List<INode> _ast;
    private List<byte> _program = [];
    private Dictionary<string, Variable> _vars = new();
    private byte _nextVar = 1;
    private VBType _priorResult = VBType.Void;
    
    private class Variable
    {
        public VBType Type;
        public byte Id;

        public Variable(VBType type, byte id)
        {
            Type = type;
            Id = id;
        }
    }

    public Compiler(Parser parser)
    {
        _ast = parser.Parse();
    }

    public ByteCode Compile()
    {
        foreach (var node in _ast)
        {
            _priorResult = ProcessNode(node);
        }

        return new ByteCode(_program.ToArray());
    }

    private byte NextVar()
    {
        return _nextVar++;
    }

    private void Operation(OpCode op)
    {
        _program.Add((byte)op);
    }

    private void Arg(byte arg)
    {
        _program.Add(arg);
    }

    private void Arg(byte[] args)
    {
        foreach (var arg in args)
        {
            _program.Add(arg);
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
                Arg(BitConverter.GetBytes(str.Length));
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
    
    private VBType ProcessNode(INode node)
    {
        switch (node)
        {
            case VarDecNode dec:
            {
                var id = NextVar();
                _vars[dec.Name] = new Variable(dec.Type, id);
                if (dec.Value is not null)
                {
                    var type = ProcessNode(dec.Value);
                    if (type != dec.Type) throw new ParseException($"A {type.ToString()} is not a {dec.Type.ToString()}.");
                    Operation (OpCode.Store);
                    Arg       (id);
                }
                return VBType.Void;
            }
            case VarSetNode setVar:
            {
                var variable = _vars[setVar.Name];
                var actualType = ProcessNode(setVar.Value);
                if (variable.Type != actualType) throw new ParseException($"A {actualType.ToString()} is not a {variable.Type.ToString()}.");
                Operation (OpCode.Store);
                Arg       (variable.Id);
                return VBType.Void;
            }
            case ValueNode val:
            {
                Operation (OpCode.Push);
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
                // Otherwise, it will be stored in local var 0
                Operation (OpCode.Load);
                Arg       (0);
                return _priorResult;
            }
            case VarRefNode varRef:
            {
                var var = _vars[varRef.Name];
                Operation (OpCode.Load);
                Arg       (var.Id);
                return var.Type;
            }
            case IfNode ifNode:
            {
                // For if's with no else's, jump over this branch if the condition is false.
                var initLength = _program.Count;
                Operation (OpCode.JumpIf);
                Arg       (0);
                foreach (INode then in ifNode.Then)
                {
                    ProcessNode(then);
                }
                var thenLength = _program.Count;
                if (thenLength > 0)
                {
                    _program[initLength+1] = (byte)(thenLength-1);                    
                }
                if (ifNode.Else is not null)
                {
                    Operation (OpCode.Jump);
                    Arg       (0);
                    foreach (INode otherwise in ifNode.Else)
                    {
                        ProcessNode(otherwise);
                    }
                    var elseLength = _program.Count;
                    if (elseLength > 0)
                    {
                        _program[thenLength+1] = (byte)(elseLength-1);                    
                    }
                }
                return VBType.Void;
            }
            case WhileLoopNode whileNode:
            {
                var returnPos = _program.Count;
                ProcessNode(whileNode.Condition);
                Operation(OpCode.Invert);
                
                Operation (OpCode.JumpIf);
                var addrPos = _program.Count;
                Arg       (0);

                foreach (INode node2 in whileNode.Loop)
                {
                    ProcessNode(node2);
                }
                var loopLength = _program.Count;
                _program[addrPos] = (byte)(loopLength+2);
                Operation (OpCode.Jump);
                Arg       ((byte) returnPos);
                return VBType.Void;
            }
        }
        throw new NotImplementedException($"Unknown node type: {node.GetType()}");
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
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't subtract two things if they aren't numbers.");
                Operation(OpCode.Sub);
                return VBType.Number;
            }
            case BinOp.Mul:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't add two things if they aren't numbers.");
                Operation(OpCode.Add);
                return VBType.Number;
            }
            case BinOp.Div:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't divide two things if they aren't numbers.");
                Operation(OpCode.Div);
                return VBType.Number;
            }
            case BinOp.Equal:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                Operation(OpCode.Equal);
                return VBType.Boolean;
            }
            case BinOp.NotEqual:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                Operation(OpCode.NotEqual);
                return VBType.Boolean;
            }
            case BinOp.GreaterThan:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.Greater);
                return VBType.Boolean;
            }
            case BinOp.LessThan:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.Less);
                return VBType.Boolean;
            }
            case BinOp.GEq:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.GreaterEqual);
                return VBType.Boolean;
            }
            case BinOp.LEq:
            {
                var arg1 = ProcessNode(node.Left);
                var arg2 = ProcessNode(node.Right);
                if (arg1 != VBType.Number || arg2 != VBType.Number)
                    throw new ParseException($"I can't compare two things if they aren't numbers.");
                Operation(OpCode.LessEqual);
                return VBType.Boolean;
            }
            default:
                throw new NotImplementedException("Binary operation not implemented.");
        }
    }

    private VBType ProcessUnaryOp(UnaryOpNode node)
    {
        switch (node.Op)
        {
            case UnaryOp.Invert:
            {
                var arg = ProcessNode(node.Expr);
                if (arg != VBType.Boolean)
                    throw new ParseException($"I can't know when a {arg.ToString()} is true or false; I can't know the opposite either.");
                Operation(OpCode.Invert);
                return VBType.Boolean;
            }
            case UnaryOp.Negate:
            {
                var arg = ProcessNode(node.Expr);
                if (arg != VBType.Boolean)
                    throw new ParseException($"I can't find the inverse to a {arg.ToString()}; it must be a number.");
                Operation(OpCode.Negate);
                return VBType.Boolean;
            }
        }
        throw new NotImplementedException("Invalid unary operator.");
    }
}