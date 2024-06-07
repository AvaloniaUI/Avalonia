namespace Tmds.DBus.Protocol;

public struct Signature
{
    private string _value;

    public Signature(string value) => _value = value;

    public override string ToString() => _value ?? "";

    public Variant AsVariant() => new Variant(this);
}