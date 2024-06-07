namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Write<T>(T value) where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            WriteByte((byte)(object)value);
        }
        else if (typeof(T) == typeof(bool))
        {
            WriteBool((bool)(object)value);
        }
        else if (typeof(T) == typeof(short))
        {
            WriteInt16((short)(object)value);
        }
        else if (typeof(T) == typeof(ushort))
        {
            WriteUInt16((ushort)(object)value);
        }
        else if (typeof(T) == typeof(int))
        {
            WriteInt32((int)(object)value);
        }
        else if (typeof(T) == typeof(uint))
        {
            WriteUInt32((uint)(object)value);
        }
        else if (typeof(T) == typeof(long))
        {
            WriteInt64((long)(object)value);
        }
        else if (typeof(T) == typeof(ulong))
        {
            WriteUInt64((ulong)(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            WriteDouble((double)(object)value);
        }
        else if (typeof(T) == typeof(string))
        {
            WriteString((string)(object)value);
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            WriteString(((ObjectPath)(object)value).ToString());
        }
        else if (typeof(T) == typeof(Signature))
        {
            WriteSignature(((Signature)(object)value).ToString());
        }
        else if (typeof(T) == typeof(Variant))
        {
            ((Variant)(object)value).WriteTo(ref this);
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            WriteHandle((SafeHandle)(object)value);
        }
        else if (typeof(T).IsAssignableTo(typeof(IDBusWritable)))
        {
            (value as IDBusWritable)!.WriteTo(ref this);
        }
        else if (Feature.IsDynamicCodeEnabled)
        {
            WriteDynamic<T>(value);
        }
        else
        {
            ThrowNotSupportedType(typeof(T));
        }
    }

    private static void ThrowNotSupportedType(Type type)
    {
        throw new NotSupportedException($"Cannot write type {type.FullName}");
    }
}
