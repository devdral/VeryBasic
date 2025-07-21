namespace VeryBasic.Runtime.Executing;

public class ExternalProcedure(ExternalProcedure.ProcedureMethod method, VBType returnType, params List<VBType> expectedArguments)
    : IProcedure
{
    public delegate Value? ProcedureMethod(List<Value> args);
    public ProcedureMethod Method = method;
    public List<VBType> ExpectedArguments { get; } = expectedArguments;
    public VBType ReturnType { get; } = returnType;

    public Value? Run(List<Value> args)
    {
        return Method(args);
    }
}