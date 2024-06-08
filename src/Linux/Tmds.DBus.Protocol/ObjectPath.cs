namespace Tmds.DBus.Protocol;

public struct ObjectPath
{
    private string _value;

    public ObjectPath(string value) => _value = value;

    public override string ToString() => _value ?? "";

    public static implicit operator string(ObjectPath value) => value._value;

    public static implicit operator ObjectPath(string value) => new ObjectPath(value);

    public Variant AsVariant() => new Variant(this);
}