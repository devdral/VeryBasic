using System.Globalization;
using VeryBasic.Runtime.Executing.Errors;
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
        }
        else if (type == typeof(bool))
        {
            this.Type = VBType.Boolean;
        }
        else if (type == typeof(string))
        {
            this.Type = VBType.String;
        }
        else if (type == typeof(Null))
        {
            this.Type = VBType.Void;
        }
        else if (type == typeof(List<Value>)) {
            this.Type = VBType.List;
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
    
    public static Value TryConvert(Value other, VBType type)
    {
        if (other.Type == type)
        {
            return other;
        }
        
        if (other.Type == VBType.String &&
            type       == VBType.Number)
        {
            var valueString = other.Get<string>();
            try
            {
                return new Value(double.Parse(valueString, CultureInfo.CurrentCulture));
            }
            catch
            {
                throw new RuntimeException("I can't understand {valueString} as a number.");
            }
        }
        
        if (other.Type == VBType.String &&
            type == VBType.Boolean)
        {
            return new Value(other.Get<string>().ToLower() switch
            {
                "yes" => true,
                "no" => false,
                "true" => true,
                "false" => false,
                _ => throw new RuntimeException("A boolean is a yes-or-no (could also be 'true' or 'false')")
            });
        }

        return From(other, type);
    }
    
    public class Null {}
}

public enum VBType
{
    Number,
    String,
    Boolean,
    // NOTE: For now, lists can have variance.
    List,
    Void
}