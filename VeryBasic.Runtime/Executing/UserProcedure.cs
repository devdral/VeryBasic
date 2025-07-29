using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime.Executing;

public class UserProcedure : IProcedure
{
    public UserProcedure(List<string> args, List<INode> body, Environment parentEnv, List<VBType> expectedArguments, VBType returnType)
    {
        this.args = args;
        this.body = body;
        this.parent = parentEnv;
        ExpectedArguments = expectedArguments;
        ReturnType = returnType;
    }

    public List<VBType> ExpectedArguments { get; }
    private List<string> args;
    public VBType ReturnType { get; }
    private List<INode> body;
    private Environment parent;
    public Value? Run(List<Value> args)
    {
        Environment env = new Environment(parent);
        TreeWalkRunner runner = new TreeWalkRunner(env);
        for (int i = 0; i < this.args.Count; i++)
        {
            env.CreateVar(this.args[i], args[i].Type, args[i]);
        }
        runner.Run(body);
        return env.TheResult;
    }
}