using VeryBasic.Runtime.Executing.Errors;

namespace VeryBasic.Runtime.Executing;

public class ExternTable
{
    public record Signature(List<VBType> Args, VBType ReturnType);

    public record ExternProcedure(string TypeName, string MethodName, Signature Signature);

    private record VBProcedure(string Name, Signature Signature, int Address);

    private Dictionary<string, ExternProcedure> _externs = new();

    public Dictionary<string, ExternProcedure> Externs => _externs;
    
    private Dictionary<string, VBProcedure> _natives = new();

    public void RegisterExtern(string nativeName, string typeName, string methodName, Signature signature)
    {
        _externs[nativeName] = new ExternProcedure(typeName, methodName, signature);
    }

    internal Value CallExtern(string name, IList<Value> args)
    {
        var externalArgs = new object[args.Count];
        var externalTypes = new Type[args.Count];
        for (var index = 0; index < args.Count; index++)
        {
            var value = args[index];
            externalArgs[index] = value.Get<object>();
            externalTypes[index] = externalArgs[index].GetType();
        }
        var procedure = _externs[name];
        var impl = Type.GetType(procedure.TypeName).GetMethod(procedure.MethodName, types: externalTypes);
        if (impl is null)
            throw new FatalException($"Invalid args for extern '{name}'.");
        if (!impl.IsStatic)
            throw new ArgumentException($"The extern specified ('{name}') is " +
                                        "implemented as non-static. The method backing " +
                                        "an extern must be static.");
        var ret = impl.Invoke(null, externalArgs);
        return procedure.Signature.ReturnType == VBType.Void ? new Value(new Value.Null()) : new Value(ret);
    }
}