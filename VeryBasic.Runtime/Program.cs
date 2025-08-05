namespace VeryBasic.Runtime;
using VeryBasic.Runtime.Executing;
using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;

public class Program
{
    public Program(string source)
    {
        _source = source;
        _parser = new Parser(_source);
    }

    public Program()
    {
        _source = null;
        _virtualMachine = new VirtualMachine(new ByteCode([]));
    }

    private string? _source;
    private Parser? _parser;
    private Compiler? _compiler;
    private ByteCode? _program;
    private VirtualMachine? _virtualMachine;

    public void Compile()
    {
        if (_source is null)
            throw new FatalException("No source provided.");
        _compiler = new Compiler(_parser);
        _program = _compiler.Compile();
        _virtualMachine = new VirtualMachine(_program);
    }

    public void Run()
    {
        if (_virtualMachine is null)
            throw new FatalException("Program not compiled. Please call Compile before calling Run.");
        _virtualMachine.Run();
    }

    public void RunCode(string code)
    {
        _source = code;
        _parser = new Parser(_source);
        _compiler = new Compiler(_parser);
        _program = _compiler.Compile();
        // Hot-swap the virtual machine's source
        // without erasing its state.
        _virtualMachine.Program = _program;
        _virtualMachine.Run();
    }
}