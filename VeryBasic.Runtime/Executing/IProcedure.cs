namespace VeryBasic.Runtime.Executing;

public interface IProcedure
{
    public List<VBType> ExpectedArguments { get; }
    public VBType ReturnType { get; }
    public Value? Run(List<Value> args);
}