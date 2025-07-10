using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime;

class Program
{
    static void Main(string[] args)
    {
        Tokenizer tok = new Tokenizer("""
                                print "Hello world!"
                                create variable x, a number, from 32
                                calculate 10+x
                                print the result
                                if x < 50 then
                                    print "Less than 50!"
                                done
                                """);
        foreach (IToken token in tok.Tokenize())
        {
            Console.WriteLine(token);
        }
    }
}