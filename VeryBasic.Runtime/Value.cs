namespace VeryBasic.Runtime;

public class Value
{
    public VBType Type;
    private object _value;
    public object AsObject => _value;

    public T Get<T>()
    {
        return (T)_value;
    }

    public bool Equals(Value obj)
    {
        return this.Type == obj.Type && this._value == obj._value;
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
        }
        else
        {
            throw new InvalidCastException();
        }

        _value = value;
    }
}

public enum VBType
{
    Number,
    String,
    Boolean,
}