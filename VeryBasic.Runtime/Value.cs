using System.Globalization;
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

    public static Value From(Value other, VBType type)
    {
        if (other.Type == type)
        {
            return other;
        }
        
        if (other.Type == VBType.Number &&
            type       == VBType.String)
        {
            return new Value(other.Get<double>().ToString(CultureInfo.CurrentCulture));
        }
        
        if (other.Type == VBType.Boolean &&
                   type == VBType.String)
        {
            return new Value(other.Get<bool>() switch
            {
                true => "Yes!",
                false => "No.",
            });
        }
        
        throw new InvalidCastException($"I can't turn a {other.Type} into a {type}");
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