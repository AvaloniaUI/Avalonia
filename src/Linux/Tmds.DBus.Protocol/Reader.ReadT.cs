namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Read<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)ReadByte();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)ReadBool();
        }
        else if (typeof(T) == typeof(short))
        {
            return (T)(object)ReadInt16();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T)(object)ReadUInt16();
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)ReadInt32();
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T)(object)ReadUInt32();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T)(object)ReadInt64();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T)(object)ReadUInt64();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)ReadDouble();
        }
        else if (typeof(T) == typeof(string))
        {
            return (T)(object)ReadString();
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return (T)(object)ReadObjectPath();
        }
        else if (typeof(T) == typeof(Signature))
        {
            return (T)(object)ReadSignatureAsSignature();
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            return (T)(object)ReadHandleGeneric<T>()!;
        }
        else if (typeof(T) == typeof(VariantValue))
        {
            return (T)(object)ReadVariantValue();
        }
        else if (Feature.IsDynamicCodeEnabled)
        {
            return ReadDynamic<T>();
        }

        ThrowNotSupportedType(typeof(T));
        return default!;
    }

    private static void ThrowNotSupportedType(Type type)
    {
        throw new NotSupportedException($"Cannot read type {type.FullName}");
    }
}
