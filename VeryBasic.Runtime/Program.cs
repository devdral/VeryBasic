namespace VeryBasic.Runtime;
using Executing;
using Executing.Errors;
using Parsing;

public class Program
{
    public Program(string source, ExternTable environment)
    {
        _source = source;
        _environment = environment;
        _compiler = new Compiler();
        _compiler.RegisterExterns(_environment);
        _parser = new Parser(_source);
    }

    public Program(ExternTable environment)
    {
        _source = null;
        _environment = environment;
        _compiler = new Compiler();
        _compiler.RegisterExterns(_environment);
        _virtualMachine = new VirtualMachine(new ByteCode([]), _environment);
    }

    private string? _source;
    private Parser? _parser;
    private Compiler? _compiler;
    private ByteCode? _program;
    private VirtualMachine? _virtualMachine;
    private ExternTable _environment;

    public void Compile()
    {
        if (_source is null)
            throw new FatalException("No source provided.");
        foreach (var proc in _environment.Externs.Keys)
        {
            _parser.RegisterPreexistingProcedure(proc);
        }
        _program = _compiler.Compile(_parser);
        _virtualMachine = new VirtualMachine(_program, _environment);
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
        if (_compiler is null)
            _compiler = new Compiler();
        foreach (var proc in _environment.Externs.Keys)
        {
            _parser.RegisterPreexistingProcedure(proc);
        }
        _program = _compiler.Compile(_parser);
        // Hot-swap the virtual machine's source
        // without erasing its state.
        _virtualMachine.Program = _program;
        _virtualMachine.Run();
    }
}