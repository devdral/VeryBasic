namespace VeryBasic.Runtime.Executing.Errors;

public class FatalException : Exception
{
    public FatalException(string message) : base(message) {}
}