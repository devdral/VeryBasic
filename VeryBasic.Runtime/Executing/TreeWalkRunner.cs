using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class TreeWalkRunner : IVisitor<Value>
{
    private List<INode> _ast;

    private Environment _env;
    
    public static readonly Value VBNull = new (new Value.Null());

    public TreeWalkRunner(Environment env)
    {
        _env = env;
    }
    public void Run(Parser parser)
    {
        try
        {
            _ast = parser.Parse();
        }
        catch (Exception e)
        {
            throw new ParseException(e.Message);
        }

        foreach (var node in _ast)
        {
            try
            {
                _env.TheResult = node.Accept(this);
            }
            catch (Exception e)
            {
                throw new RuntimeException(e.Message); 
            }
        }
    }

    public void Run(List<INode> stmts)
    {
        foreach (var node in stmts)
        {
            try
            {
                _env.TheResult = node.Accept(this);
            }
            catch (Exception e)
            {
                throw new RuntimeException(e.Message); 
            }
        }
    }

    public Value VisitValueNode(ValueNode node)
    {
        return node.Value;
    }

    public Value VisitUnaryOpNode(UnaryOpNode node)
    {
        Value value = node.Expr.Accept(this);
        return node.Op switch
        {
            UnaryOp.Invert when value.Type == VBType.Boolean => new Value(!value.Get<bool>()),
            UnaryOp.Invert => throw new Exception(
                $"I can't say when a {value.Type} is false because it's not a boolean (yes-or-no)."),
            UnaryOp.Negate when value.Type == VBType.Number => new Value(-value.Get<double>()),
            UnaryOp.Negate => throw new Exception($"I can't give the opposite sign of {value.Type}; it's not a number."),
        };
    }

    public Value VisitBinaryOpNode(BinaryOpNode node)
    {
        Value left = node.Left.Accept(this);
        Value right = node.Right.Accept(this);
        return node.Op switch
        {
            BinOp.Add when left.Type == VBType.Number && right.Type == VBType.Number => new Value(left.Get<double>() +
                right.Get<double>()),
            BinOp.Add when left.Type == VBType.String && right.Type == VBType.String => new Value(left.Get<string>() +
                right.Get<string>()),
            BinOp.Add => throw new Exception("I can't add two things if they aren't both numbers."),
            BinOp.Sub when left.Type == VBType.Number && right.Type == VBType.Number => new Value(left.Get<double>() -
                right.Get<double>()),
            BinOp.Sub => throw new Exception("I can't subtract two things if they aren't numbers."),
            BinOp.Mul when left.Type == VBType.Number && right.Type == VBType.Number => new Value(left.Get<double>() *
                right.Get<double>()),
            BinOp.Mul => throw new Exception("I can't multiply two things if they aren't numbers."),
            BinOp.Div when left.Type == VBType.Number && right.Type == VBType.Number => new Value(left.Get<double>() /
                right.Get<double>()),
            BinOp.Div => throw new Exception("I can't divide two things if they aren't numbers."),
            BinOp.And when left.Type == VBType.Boolean && right.Type == VBType.Boolean => new Value(left.Get<bool>() &&
                right.Get<bool>()),
            BinOp.And => throw new Exception(
                "I can't see when two things are true if they aren't both booleans (yes's-or-no's)."),
            BinOp.Or when left.Type == VBType.Boolean && right.Type == VBType.Boolean => new Value(left.Get<bool>() ||
                right.Get<bool>()),
            BinOp.Or => throw new Exception(
                "I can't see when either of two things are true if they aren't both booleans (yes's-or-no's)."),
            BinOp.Equal => new Value(left.Equals(right)),
            BinOp.NotEqual => new Value(!left.Equals(right)),
            BinOp.LessThan when left.Type == VBType.Number && right.Type == VBType.Number => new Value(
                left.Get<double>() < right.Get<double>()),
            BinOp.LessThan => throw new Exception(
                "I can't see when one thing is less than another if they aren't both numbers."),
            BinOp.GreaterThan when left.Type == VBType.Number && right.Type == VBType.Number => new Value(
                left.Get<double>() > right.Get<double>()),
            BinOp.GreaterThan => throw new Exception(
                "I can't see when one thing is greater than another if they aren't both numbers."),
            BinOp.LEq when left.Type == VBType.Number && right.Type == VBType.Number => new Value(left.Get<double>() <=
                right.Get<double>()),
            BinOp.LEq => throw new Exception(
                "I can't see when one thing is less than (or equal) to another if they aren't both numbers."),
            BinOp.GEq when left.Type == VBType.Number && right.Type == VBType.Number => new Value(left.Get<double>() >=
                right.Get<double>()),
            BinOp.GEq => throw new Exception(
                "I can't see when one thing is greater than (or equal) to another if they aren't both numbers."),
        };
    }

    public Value VisitTheResultNode(TheResultNode node)
    {
        if (_env.TheResult == VBNull)
        {
            throw new Exception("The prior statement did not result in anything.");
        }
        return _env.TheResult;
    }

    public Value VisitVarDecNode(VarDecNode node)
    {
        if (node.Value == null)
            _env.CreateVar(node.Name, node.Type);
        else
            _env.CreateVar(node.Name, node.Type, node.Value.Accept(this));
        return VBNull;
    }

    public Value VisitVarSetNode(VarSetNode node)
    {
        _env.SetVar(node.Name, node.Value.Accept(this));
        return VBNull;
    }

    public Value? VisitProcCallNode(ProcCallNode node)
    {
        List<Value> args = [];
        foreach (var arg in node.Args)
        {
            args.Add(arg.Accept(this));
        }
        return _env.CallProc(node.Name, args);
    }

    public Value VisitVarRefNode(VarRefNode node)
    {
        return _env.GetVar(node.Name);
    }

    public Value VisitIfNode(IfNode node)
    {
        Value cond = node.Condition.Accept(this);
        if (cond.Type != VBType.Boolean) throw new Exception("An 'if' statement cannot check 'if' something that is not a yes-or-no is true.");
        if (cond.Get<bool>())
        {
            foreach (INode statement in node.Then)
            {
                statement.Accept(this);
            }
        }
        else if (node.Else != null)
        {
            foreach (INode statement in node.Else)
            {
                statement.Accept(this);
            }
        }
        return VBNull;
    }

    public Value VisitWhileLoopNode(WhileLoopNode node)
    {
        while (node.Condition
               .Accept(this)
               .Get<bool>()
               )
        {
            foreach (INode statement in node.Loop)
            {
                statement.Accept(this);
            }
        }
        return VBNull;
    }

    public Value VisitRepeatLoopNode(RepeatLoopNode node)
    {
        double timesAsDouble = node.Times
            .Accept(this)
            .Get<double>();
        if (timesAsDouble % 1 != 0) throw new Exception("You can't do something a fractional number of times.");
        int times = (int)timesAsDouble;
        for (int i = 0; i < times; i++)
        {
            foreach (INode statement in node.Loop)
            {
                statement.Accept(this);
            }
        }
        return VBNull;
    }

    public Value VisitListNode(ListNode node)
    {
        List<Value> values = [];
        foreach (var elem in node.Items)
        {
            values.Add(elem.Accept(this));
        }
        return new Value(values);
    }

    public Value VisitListGetNode(ListGetNode node)
    {
        var list = Value.From(node.List.Accept(this), VBType.List).Get<List<Value>>();
        var indexAsDouble = Value.From(node.Index.Accept(this), VBType.Number).Get<double>();
        if (indexAsDouble % 1 != 0) throw new Exception("List items are numbered with whole numbers (1, 2, 3, 4...).");
        int index = (int)indexAsDouble;
        if (index > list.Count)
        {
            throw new Exception("That item number went beyond the end of the list.");
        } else if (index < 1)
        {
            throw new Exception("List item numbers start from one.");
        }
        return list[index - 1];
    }

    public Value VisitListSetNode(ListSetNode node)
    {
        var list = Value.From(node.List.Accept(this), VBType.List).Get<List<Value>>();
        var indexAsDouble = Value.From(node.Index.Accept(this), VBType.Number).Get<double>();
        if (indexAsDouble % 1 != 0) throw new Exception("List items are numbered with whole numbers (1, 2, 3, 4...).");
        int index = (int)indexAsDouble;
        Value value = node.Value.Accept(this);
        if (index > list.Count)
        {
            throw new Exception("That item number went beyond the end of the list.");
        } else if (index < 1)
        {
            throw new Exception("List item numbers start from one.");
        }
        list[index] = value;
        return VBNull;
    }

    public Value VisitProcDefNode(ProcDefNode node)
    {
        _env.CreateProc(node.Name,
            new UserProcedure(node.Args,
                node.Body,
                _env,
                node.ExpectedArgs,
                node.ReturnType));
        return VBNull;
    }
}