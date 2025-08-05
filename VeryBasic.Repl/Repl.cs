using VeryBasic.Runtime.Executing;
using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;
using Environment = VeryBasic.Runtime.Executing.Environment;

namespace VeryBasic.Repl;

public class Repl
{
    private VeryBasic.Runtime.Program _runner = new();

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