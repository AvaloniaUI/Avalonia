namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    [RequiresUnreferencedCode(Strings.UseNonObjectWriteVariant)]
    [Obsolete(Strings.UseNonObjectWriteVariantObsolete)]
    public void WriteVariant(object value)
    {
        Type type = value.GetType();

        if (type == typeof(byte))
        {
            WriteVariantByte((byte)value);
            return;
        }
        else if (type == typeof(bool))
        {
            WriteVariantBool((bool)value);
            return;
        }
        else if (type == typeof(short))
        {
            WriteVariantInt16((short)value);
            return;
        }
        else if (type == typeof(ushort))
        {
            WriteVariantUInt16((ushort)value);
            return;
        }
        else if (type == typeof(int))
        {
            WriteVariantInt32((int)value);
            return;
        }
        else if (type == typeof(uint))
        {
            WriteVariantUInt32((uint)value);
            return;
        }
        else if (type == typeof(long))
        {
            WriteVariantInt64((long)value);
            return;
        }
        else if (type == typeof(ulong))
        {
            WriteVariantUInt64((ulong)value);
            return;
        }
        else if (type == typeof(double))
        {
            WriteVariantDouble((double)value);
            return;
        }
        else if (type == typeof(string))
        {
            WriteVariantString((string)value);
            return;
        }
        else if (type == typeof(ObjectPath))
        {
            WriteVariantObjectPath(value.ToString()!);
            return;
        }
        else if (type == typeof(Signature))
        {
            WriteVariantSignature(value.ToString()!);
            return;
        }
        else
        {
            var typeWriter = TypeWriters.GetTypeWriter(type);
            typeWriter.WriteVariant(ref this, value);
        }
    }
}
