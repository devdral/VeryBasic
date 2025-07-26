using VeryBasic.Runtime.Executing;
using VeryBasic.Runtime.Executing.Errors;
using VeryBasic.Runtime.Parsing;
using Environment = VeryBasic.Runtime.Executing.Environment;

namespace VeryBasic.Repl;

public class Repl
{
    public TreeWalkRunner Runner { get; private set; } = new TreeWalkRunner(Environment.Default());

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
                Parser parser = new Parser(program);
                try
                {
                    Runner.Run(parser);
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