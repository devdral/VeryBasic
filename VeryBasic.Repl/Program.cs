using System.Diagnostics;
using VeryBasic.Repl;
using VeryBasic.Runtime.Parsing;
using VeryBasic.Runtime.Executing;

if (args.Length >= 1)
{
    var program = File.ReadAllText(args[0]);
    var runner = new VeryBasic.Runtime.Program(program, Repl.DefaultEnv());
    var stwch = Stopwatch.StartNew();
    runner.Compile();
    Console.WriteLine($"Compiled in {stwch.ElapsedMilliseconds}ms.");
    stwch.Stop();
    runner.Run();
}
else
{
    Repl repl = new Repl();
    repl.Start();
}