using VeryBasic.Runtime;
using VeryBasic.Runtime.Executing;
using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Repl;

public class Repl
{
    private VeryBasic.Runtime.Program _runner = new(DefaultEnv());

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
                    _runner.RunCode(program);
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