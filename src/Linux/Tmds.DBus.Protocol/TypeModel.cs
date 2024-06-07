namespace Tmds.DBus.Protocol;

static partial class TypeModel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DBusType GetTypeAlignment<T>()
    {
        // TODO (perf): add caching.
        if (typeof(T) == typeof(byte))
        {
            return DBusType.Byte;
        }
        else if (typeof(T) == typeof(bool))
        {
            return DBusType.Bool;
        }
        else if (typeof(T) == typeof(short))
        {
            return DBusType.Int16;
        }
        else if (typeof(T) == typeof(ushort))
        {
            return DBusType.UInt16;
        }
        else if (typeof(T) == typeof(int))
        {
            return DBusType.Int32;
        }
        else if (typeof(T) == typeof(uint))
        {
            return DBusType.UInt32;
        }
        else if (typeof(T) == typeof(long))
        {
            return DBusType.Int64;
        }
        else if (typeof(T) == typeof(ulong))
        {
            return DBusType.UInt64;
        }
        else if (typeof(T) == typeof(double))
        {
            return DBusType.Double;
        }
        else if (typeof(T) == typeof(string))
        {
            return DBusType.String;
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return DBusType.ObjectPath;
        }
        else if (typeof(T) == typeof(Signature))
        {
            return DBusType.Signature;
        }
        else if (typeof(T) == typeof(Variant))
        {
            return DBusType.Variant;
        }
        else if (typeof(T).IsConstructedGenericType)
        {
            Type type = typeof(T).GetGenericTypeDefinition();
            if (type == typeof(Dict<,>))
            {
                return DBusType.Array;
            }
            else if (type == typeof(Array<>))
            {
                return DBusType.Array;
            }
            else if (type == typeof(Struct<>) ||
                     type == typeof(Struct<,>) ||
                     type == typeof(Struct<,,>) ||
                     type == typeof(Struct<,,,>) ||
                     type == typeof(Struct<,,,,>) ||
                     type == typeof(Struct<,,,,,>) ||
                     type == typeof(Struct<,,,,,,>) ||
                     type == typeof(Struct<,,,,,,,>) ||
                     type == typeof(Struct<,,,,,,,,>) ||
                     type == typeof(Struct<,,,,,,,,,>))
            {
                return DBusType.Struct;
            }
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            return DBusType.UnixFd;
        }
        else if (Feature.IsDynamicCodeEnabled)
        {
            return GetTypeAlignmentDynamic<T>();
        }

        ThrowNotSupportedType(typeof(T));
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureSupportedVariantType<T>()
    {
        if (typeof(T) == typeof(byte))
        { }
        else if (typeof(T) == typeof(bool))
        { }
        else if (typeof(T) == typeof(short))
        { }
        else if (typeof(T) == typeof(ushort))
        { }
        else if (typeof(T) == typeof(int))
        { }
        else if (typeof(T) == typeof(uint))
        { }
        else if (typeof(T) == typeof(long))
        { }
        else if (typeof(T) == typeof(ulong))
        { }
        else if (typeof(T) == typeof(double))
        { }
        else if (typeof(T) == typeof(string))
        { }
        else if (typeof(T) == typeof(ObjectPath))
        { }
        else if (typeof(T) == typeof(Signature))
        { }
        else if (typeof(T) == typeof(Variant))
        { }
        else if (typeof(T).IsConstructedGenericType)
        {
            Type type = typeof(T).GetGenericTypeDefinition();
            if (type == typeof(Dict<,>) ||
                type == typeof(Array<>) ||
                type == typeof(Struct<>) ||
                type == typeof(Struct<,>) ||
                type == typeof(Struct<,,>) ||
                type == typeof(Struct<,,,>) ||
                type == typeof(Struct<,,,,>) ||
                type == typeof(Struct<,,,,,>) ||
                type == typeof(Struct<,,,,,,>) ||
                type == typeof(Struct<,,,,,,,>) ||
                type == typeof(Struct<,,,,,,,,>) ||
                type == typeof(Struct<,,,,,,,,,>))
            {
                foreach (var innerType in type.GenericTypeArguments)
                {
                    EnsureSupportedVariantType(innerType);
                }
            }
            else
            {
                ThrowNotSupportedType(typeof(T));
            }
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        { }
        else
        {
            ThrowNotSupportedType(typeof(T));
        }
    }

    private static void EnsureSupportedVariantType(Type type)
    {
        if (type == typeof(byte))
        { }
        else if (type == typeof(bool))
        { }
        else if (type == typeof(short))
        { }
        else if (type == typeof(ushort))
        { }
        else if (type == typeof(int))
        { }
        else if (type == typeof(uint))
        { }
        else if (type == typeof(long))
        { }
        else if (type == typeof(ulong))
        { }
        else if (type == typeof(double))
        { }
        else if (type == typeof(string))
        { }
        else if (type == typeof(ObjectPath))
        { }
        else if (type == typeof(Signature))
        { }
        else if (type == typeof(Variant))
        { }
        else if (type.IsConstructedGenericType)
        {
            Type typeDefinition = type.GetGenericTypeDefinition();
            if (typeDefinition == typeof(Dict<,>) ||
                typeDefinition == typeof(Array<>) ||
                typeDefinition == typeof(Struct<>) ||
                typeDefinition == typeof(Struct<,>) ||
                typeDefinition == typeof(Struct<,,>) ||
                typeDefinition == typeof(Struct<,,,>) ||
                typeDefinition == typeof(Struct<,,,,>) ||
                typeDefinition == typeof(Struct<,,,,,>) ||
                typeDefinition == typeof(Struct<,,,,,,>) ||
                typeDefinition == typeof(Struct<,,,,,,,>) ||
                typeDefinition == typeof(Struct<,,,,,,,,>) ||
                typeDefinition == typeof(Struct<,,,,,,,,,>))
            {
                foreach (var innerType in typeDefinition.GenericTypeArguments)
                {
                    EnsureSupportedVariantType(innerType);
                }
            }
            else
            {
                ThrowNotSupportedType(type);
            }
        }
        else if (type.IsAssignableTo(typeof(SafeHandle)))
        { }
        else
        {
            ThrowNotSupportedType(type);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Utf8Span GetSignature<T>(scoped Span<byte> buffer)
    {
        Debug.Assert(buffer.Length >= ProtocolConstants.MaxSignatureLength);

        int bytesWritten = AppendTypeSignature(typeof(T), buffer);
        return new Utf8Span(buffer.Slice(0, bytesWritten).ToArray());
    }

    private static int AppendTypeSignature(Type type, Span<byte> signature)
    {
        if (type == typeof(byte))
        {
            signature[0] = (byte)DBusType.Byte;
            return 1;
        }
        else if (type == typeof(bool))
        {
            signature[0] = (byte)DBusType.Bool;
            return 1;
        }
        else if (type == typeof(short))
        {
            signature[0] = (byte)DBusType.Int16;
            return 1;
        }
        else if (type == typeof(ushort))
        {
            signature[0] = (byte)DBusType.UInt16;
            return 1;
        }
        else if (type == typeof(int))
        {
            signature[0] = (byte)DBusType.Int32;
            return 1;
        }
        else if (type == typeof(uint))
        {
            signature[0] = (byte)DBusType.UInt32;
            return 1;
        }
        else if (type == typeof(long))
        {
            signature[0] = (byte)DBusType.Int64;
            return 1;
        }
        else if (type == typeof(ulong))
        {
            signature[0] = (byte)DBusType.UInt64;
            return 1;
        }
        else if (type == typeof(double))
        {
            signature[0] = (byte)DBusType.Double;
            return 1;
        }
        else if (type == typeof(string))
        {
            signature[0] = (byte)DBusType.String;
            return 1;
        }
        else if (type == typeof(ObjectPath))
        {
            signature[0] = (byte)DBusType.ObjectPath;
            return 1;
        }
        else if (type == typeof(Signature))
        {
            signature[0] = (byte)DBusType.Signature;
            return 1;
        }
        else if (type == typeof(Variant))
        {
            signature[0] = (byte)DBusType.Variant;
            return 1;
        }
        else if (type.IsConstructedGenericType)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(Dict<,>))
            {
                int length = 0;
                signature[length++] = (byte)'a';
                signature[length++] = (byte)'{';
                length += AppendTypeSignature(type.GenericTypeArguments[0], signature.Slice(length));
                length += AppendTypeSignature(type.GenericTypeArguments[1], signature.Slice(length));
                signature[length++] = (byte)'}';
                return length;
            }
            else if (genericTypeDefinition == typeof(Array<>))
            {
                int length = 0;
                signature[length++] = (byte)'a';
                length += AppendTypeSignature(type.GenericTypeArguments[0], signature.Slice(length));
                return length;
            }
            else if (genericTypeDefinition == typeof(Struct<>) ||
                     genericTypeDefinition == typeof(Struct<,>) ||
                     genericTypeDefinition == typeof(Struct<,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,,,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,,,,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,,,,,,>) ||
                     genericTypeDefinition == typeof(Struct<,,,,,,,,,>))
            {
                int length = 0;
                signature[length++] = (byte)'(';
                foreach (var innerType in type.GenericTypeArguments)
                {
                    length += AppendTypeSignature(innerType, signature.Slice(length));
                }
                signature[length++] = (byte)')';
                return length;
            }
        }
        else if (type.IsAssignableTo(typeof(SafeHandle)))
        {
            signature[0] = (byte)DBusType.UnixFd;
            return 1;
        }
        else if (Feature.IsDynamicCodeEnabled)
        {
            return AppendTypeSignatureDynamic(type, signature);
        }

        ThrowNotSupportedType(type);
        return 0;
    }

    private static void ThrowNotSupportedType(Type type)
    {
        throw new NotSupportedException($"Unsupported type {type.FullName}");
    }
}