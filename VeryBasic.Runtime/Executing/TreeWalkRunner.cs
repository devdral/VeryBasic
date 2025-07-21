using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class TreeWalkRunner : IVisitor<Value>
{
    private List<INode> _ast;

    private Environment _env;
    
    private static readonly Value VBNull = new (new Value.Null());

    public TreeWalkRunner()
    {
        _env = Environment.Default();
    }
    public void Run(Ast ast)
    {
        _ast = ast.Parse();
        foreach (var node in _ast)
        {
            node.Accept(this);
        }
    }

    public Value VisitValueNode(ValueNode node)
    {
        return node.Value;
    }

    public Value VisitUnaryOpNode(UnaryOpNode node)
    {
        Value value = node.Expr.Accept(this);
        if (node.Op == UnaryOp.Invert)
        {
            if (value.Type == VBType.Boolean)
            {
                return new Value(!value.Get<bool>());
            }
            throw new Exception();
        }
        
        if (node.Op == UnaryOp.Negate)
        {
            if (value.Type == VBType.Number)
            {
                return new Value(-value.Get<double>());
            }
            
            throw new Exception();
        }
        
        throw new Exception();
    }

    public Value VisitBinaryOpNode(BinaryOpNode node)
    {
        Value left = node.Left.Accept(this);
        Value right = node.Right.Accept(this);
        if (node.Op == BinOp.Add)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() + right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Sub)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() - right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Mul)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() * right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Div)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() / right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.And)
        {
            if (left.Type == VBType.Boolean && right.Type == VBType.Boolean)
            {
                return new Value(left.Get<bool>() && right.Get<bool>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Or)
        {
            if (left.Type == VBType.Boolean && right.Type == VBType.Boolean)
            {
                return new Value(left.Get<bool>() || right.Get<bool>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.Equal)
        {
            return new Value(left.Equals(right));
        }
        
        if (node.Op == BinOp.LessThan)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() < right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.GreaterThan)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() > right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.LEq)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() <= right.Get<double>());
            }
            throw new Exception();
        }
        
        if (node.Op == BinOp.GEq)
        {
            if (left.Type == VBType.Number && right.Type == VBType.Number)
            {
                return new Value(left.Get<double>() >= right.Get<double>());
            }
            throw new Exception();
        }
        
        throw new Exception();
    }

    public Value VisitTheResultNode(TheResultNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitVarDecNode(VarDecNode node)
    {
        _env.CreateVar(node.Name, node.Type);
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
}