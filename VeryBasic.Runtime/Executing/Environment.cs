using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class Environment
{
    public class Variable(VBType type, Value? value)
    {
        public VBType Type = type;
        public Value? Value = value;
    }
    
    private Dictionary<string, Variable> _vars = new ();
    private Dictionary<string, IProcedure> _procs = new ();
    public Value TheResult = TreeWalkRunner.VBNull;

    public void CreateVar(string name, VBType type)
    {
        _vars[name] = new Variable(type, TreeWalkRunner.VBNull);
    }
    
    public void CreateVar(string name, VBType type, Value value)
    {
        _vars[name] = new Variable(type, value);
    }

    public void SetVar(string name, Value value)
    {
        if (!_vars.TryGetValue(name, out Variable? var)) throw new Exception($"The variable {name} hasn't been created yet.");
        var.Value = Value.From(value, var.Type);
    }

    public Value GetVar(string name)
    {
        if (!_vars.TryGetValue(name, out Variable? value)) throw new Exception($"The variable {name} hasn't been created yet.");
        return value.Value;
    }

    public void CreateProc(string name, IProcedure proc)
    {
        _procs[name] = proc;
    }

    public Value? CallProc(string name, List<Value> arguments)
    {
        if (!_procs.TryGetValue(name, out IProcedure? proc)) throw new Exception($"I don't know how to '{name}' in that way.");
        if (arguments.Count < proc.ExpectedArguments.Count)
            throw new Exception($"You put too few things for me to have used '{name}'.");
        for (int i = 0; i < arguments.Count; i++)
        {
            arguments[i] = Value.From(arguments[i], proc.ExpectedArguments[i]);
        }

        return proc.Run(arguments);
    }

    public static Environment Default()
    {
        var env = new Environment();

        Value Print(List<Value> args)
        {
            Console.WriteLine(args[0].Get<string>());
            return TreeWalkRunner.VBNull;
        }
        var printProc = new ExternalProcedure(Print, VBType.Void, VBType.String);
        env.CreateProc("print", printProc);

        Value Input(List<Value> args)
        {
            Console.Write("?");
            return new Value(Console.ReadLine());
        }
        var inputProc = new ExternalProcedure(Input, VBType.String);
        env.CreateProc("take input", inputProc);
        return env;
    }
}