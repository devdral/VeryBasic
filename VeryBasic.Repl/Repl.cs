using VeryBasic.Runtime;
using VeryBasic.Runtime.Executing;
using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Repl;

public class Repl
{
    private ExternTable _env = DefaultEnv();
    private Parser? _parser;
    private Compiler? _compiler;
    private VirtualMachine? _runner;

    public static ExternTable DefaultEnv()
    {
        var env = new ExternTable();
        var name = typeof(ExternImpls).AssemblyQualifiedName;
        env.RegisterExtern("print",
            name,
            nameof(ExternImpls.Print),
            new ExternTable.Signature([
                    VBType.String
                ],
                VBType.Void));
        env.RegisterExtern("ask",
            name,
            nameof(ExternImpls.PromptUser),
            new ExternTable.Signature([
                    VBType.String
                ],
                VBType.String));
        return env;
    }

    public static class ExternImpls
    {
        public static void Print(string msg)
        {
            Console.WriteLine(msg);
        }

        public static string PromptUser(string prompt)
        {
            Console.Write(prompt);
            var response = Console.ReadLine();
            if (response is null)
                throw new RuntimeException("I can't seem to take the user's input!");
            return response;
        }
    }

    private void RunCode(string program)
    {
        if (_compiler is null)
        {
            _parser = new Parser(program);
            foreach (var proc in _env.Externs)
            {
                _parser.RegisterPreexistingProcedure(proc.Key, proc.Value.Signature.Args.Count);
            }
            _compiler = new Compiler();
            _compiler.RegisterExterns(_env);
            var code = _compiler.Compile(_parser);
            _runner = new VirtualMachine(code, _env);
            _runner.Run();
        }
        else
        {
            _parser.Code = program;
            var code = _compiler.CompileMore(_parser);
            _runner.Program = code;
            _runner.Run();
        }
    }

    public void Start()
    {
        Console.Write(">>");
        string userCommand = Console.ReadLine();
        string program = "";
        while (true)
        {
            if (userCommand == "exit")
            {
                return;
            }

            if (userCommand.EndsWith('\\'))
            {
                program += userCommand[..^1] + "\n";
                Console.Write("..");
            }
            else
            {
                program += userCommand;
                try
                {
                    RunCode(program);
                }
                catch (ParseException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
                catch (RuntimeException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }

                program = "";
                Console.Write(">>");
            }
            
            userCommand = Console.ReadLine();           
        }
    }
}