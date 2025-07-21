using VeryBasic.Runtime.Parsing;

namespace VeryBasic.Runtime;

public class Value
{
    public VBType Type;
    private object _value;

    public T Get<T>()
    {
        if (Type == VBType.Void)
        {
            throw new InvalidCastException("Cannot get value of Void.");
        }
        return (T)_value;
    }

    public bool Equals(Value obj)
    {
        return this.Type == obj.Type && this._value.Equals(obj._value);
    }

    public Value(object value)
    {
        Type type = value.GetType();
        if (type == typeof(double))
        {
            this.Type = VBType.Number;
        } else if (type == typeof(bool))
        {
            this.Type = VBType.Boolean;
        } else if (type == typeof(string))
        {
            this.Type = VBType.String;
        } else if (type == typeof(Null))
        {
            this.Type = VBType.Void;
        }
        else
        {
            throw new InvalidCastException();
        }

        _value = value;
    }
    
    public class Null {}
}

public enum VBType
{
    Number,
    String,
    Boolean,
    Void
}