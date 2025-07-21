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

    public void CreateVar(string name, VBType type)
    {
        _vars[name] = new Variable(type, null);
    }
    
    public void CreateVar(string name, VBType type, Value value)
    {
        _vars[name] = new Variable(type, value);
    }

    public void SetVar(string name, Value value)
    {
        Variable var = _vars[name];
        var.Value = Value.From(value, var.Type);
    }

    public Value GetVar(string name)
    {
        return _vars[name].Value;
    }

    public void CreateProc(string name, IProcedure proc)
    {
        _procs[name] = proc;
    }

    public Value? CallProc(string name, List<Value> arguments)
    {
        IProcedure proc = _procs[name];
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

        Value? Print(List<Value> args)
        {
            Console.WriteLine(args[0].Get<string>());
            return null;
        }
        var printProc = new ExternalProcedure(Print, VBType.Void, VBType.String);
        env.CreateProc("print", printProc);
        return env;
    }
}