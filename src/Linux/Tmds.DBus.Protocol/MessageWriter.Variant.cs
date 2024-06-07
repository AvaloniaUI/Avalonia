namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteVariant(Variant value)
    {
        value.WriteTo(ref this);
    }
}
