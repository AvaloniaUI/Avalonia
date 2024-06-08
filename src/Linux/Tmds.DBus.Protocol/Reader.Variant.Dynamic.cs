namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    [RequiresUnreferencedCode(Strings.UseNonObjectReadVariantValue)]
    [Obsolete(Strings.UseNonObjectReadVariantValueObsolete)]
    public object ReadVariant() => Read<object>();
}
