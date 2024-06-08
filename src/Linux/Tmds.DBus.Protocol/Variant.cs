namespace Tmds.DBus.Protocol;

// This type is for writing so we don't need to add
// DynamicallyAccessedMemberTypes.PublicParameterlessConstructor.
#pragma warning disable IL2091

public readonly struct Variant
{
    private static readonly object Int64Type = DBusType.Int64;
    private static readonly object UInt64Type = DBusType.UInt64;
    private static readonly object DoubleType = DBusType.Double;
    private readonly object? _o;
    private readonly long    _l;

    private const int TypeShift = 8 * 7;
    // private const int SignatureFirstShift = 8 * 6;
    private const long StripTypeMask = ~(0xffL << TypeShift);

    private DBusType Type
        => DetermineType();

    public Variant(byte value)
    {
        _l = value | ((long)DBusType.Byte << TypeShift);
        _o = null;
    }
    public Variant(bool value)
    {
        _l = (value ? 1L : 0) | ((long)DBusType.Bool << TypeShift);
        _o = null;
    }
    public Variant(short value)
    {
        _l = (ushort)value | ((long)DBusType.Int16 << TypeShift);
        _o = null;
    }
    public Variant(ushort value)
    {
        _l = value | ((long)DBusType.UInt16 << TypeShift);
        _o = null;
    }
    public Variant(int value)
    {
        _l = (uint)value | ((long)DBusType.Int32 << TypeShift);
        _o = null;
    }
    public Variant(uint value)
    {
        _l = value | ((long)DBusType.UInt32 << TypeShift);
        _o = null;
    }
    public Variant(long value)
    {
        _l = value;
        _o = Int64Type;
    }
    public Variant(ulong value)
    {
        _l = (long)value;
        _o = UInt64Type;
    }
    internal unsafe Variant(double value)
    {
        _l = *(long*)&value;
        _o = DoubleType;
    }
    public Variant(string value)
    {
        _l = (long)DBusType.String << TypeShift;
        _o = value ?? throw new ArgumentNullException(nameof(value));
    }
    public Variant(ObjectPath value)
    {
        _l = (long)DBusType.ObjectPath << TypeShift;
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    public Variant(Signature value)
    {
        _l = (long)DBusType.Signature << TypeShift;
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    public Variant(SafeHandle value)
    {
        _l = (long)DBusType.UnixFd << TypeShift;
        _o = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator Variant(byte value)
        => new Variant(value);
    public static implicit operator Variant(bool value)
        => new Variant(value);
    public static implicit operator Variant(short value)
        => new Variant(value);
    public static implicit operator Variant(ushort value)
        => new Variant(value);
    public static implicit operator Variant(int value)
        => new Variant(value);
    public static implicit operator Variant(uint value)
        => new Variant(value);
    public static implicit operator Variant(long value)
        => new Variant(value);
    public static implicit operator Variant(ulong value)
        => new Variant(value);
    public static implicit operator Variant(double value)
        => new Variant(value);
    public static implicit operator Variant(string value)
        => new Variant(value);
    public static implicit operator Variant(ObjectPath value)
        => new Variant(value);
    public static implicit operator Variant(Signature value)
        => new Variant(value);
    public static implicit operator Variant(SafeHandle value)
        => new Variant(value);

    public static Variant FromArray<T>(Array<T> value)  where T : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Array<T>>(buffer), value);
    }

    public static Variant FromDict<TKey, TValue>(Dict<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Dict<TKey, TValue>>(buffer), value);
    }

    public static Variant FromStruct<T1>(Struct<T1> value)
        where T1 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2>(Struct<T1, T2> value)
        where T1 : notnull
        where T2 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3>(Struct<T1, T2, T3> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4>(Struct<T1, T2, T3, T4> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4, T5>(Struct<T1, T2, T3, T4, T5> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4, T5, T6>(Struct<T1, T2, T3, T4, T5, T6> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4, T5, T6, T7>(Struct<T1, T2, T3, T4, T5, T6, T7> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4, T5, T6, T7, T8>(Struct<T1, T2, T3, T4, T5, T6, T7, T8> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7, T8>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9>>(buffer), value);
    }

    public static Variant FromStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return new Variant(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(buffer), value);
    }

    // Dictionary, Struct, Array.
    private unsafe Variant(Utf8Span signature, IDBusWritable value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        // Store the signature in the long if it is large enough.
        if (signature.Span.Length <= 8)
        {
            long l = 0;
            Span<byte> span = new Span<byte>(&l, 8);
            signature.Span.CopyTo(span);
            if (BitConverter.IsLittleEndian)
            {
                l = BinaryPrimitives.ReverseEndianness(l);
            }

            _l = l;
            _o = value;
        }
        else
        {
            _l = (long)signature.Span[0] << TypeShift;
            _o = new ValueTuple<byte[], IDBusWritable>(signature.Span.ToArray(), value);
        }
    }

    private byte GetByte()
    {
        DebugAssertTypeIs(DBusType.Byte);
        return (byte)(_l & StripTypeMask);
    }
    private bool GetBool()
    {
        DebugAssertTypeIs(DBusType.Bool);
        return (_l & StripTypeMask) != 0;
    }
    private short GetInt16()
    {
        DebugAssertTypeIs(DBusType.Int16);
        return (short)(_l & StripTypeMask);
    }
    private ushort GetUInt16()
    {
        DebugAssertTypeIs(DBusType.UInt16);
        return (ushort)(_l & StripTypeMask);
    }
    private int GetInt32()
    {
        DebugAssertTypeIs(DBusType.Int32);
        return (int)(_l & StripTypeMask);
    }
    private uint GetUInt32()
    {
        DebugAssertTypeIs(DBusType.UInt32);
        return (uint)(_l & StripTypeMask);
    }
    private long GetInt64()
    {
        DebugAssertTypeIs(DBusType.Int64);
        return _l;
    }
    private ulong GetUInt64()
    {
        DebugAssertTypeIs(DBusType.UInt64);
        return (ulong)(_l);
    }
    private unsafe double GetDouble()
    {
        DebugAssertTypeIs(DBusType.Double);
        double value;
        *(long*)&value = _l;
        return value;
    }
    private string GetString()
    {
        DebugAssertTypeIs(DBusType.String);
        return (_o as string)!;
    }
    private string GetObjectPath()
    {
        DebugAssertTypeIs(DBusType.ObjectPath);
        return (_o as string)!;
    }
    private string GetSignature()
    {
        DebugAssertTypeIs(DBusType.Signature);
        return (_o as string)!;
    }
    private SafeHandle GetUnixFd()
    {
        DebugAssertTypeIs(DBusType.UnixFd);
        return (_o as SafeHandle)!;
    }

    private void DebugAssertTypeIs(DBusType expected)
    {
        Debug.Assert(Type == expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DBusType DetermineType()
    {
        // For most types, we store the DBusType in the highest byte of the long.
        // Except for some types, like Int64, for which we store the value allocation free
        // in the long, and use the object field to store the type.
        DBusType type = (DBusType)(_l >> TypeShift);
        if (_o is not null)
        {
            if (_o.GetType() == typeof(DBusType))
            {
                type = (DBusType)_o;
            }
        }
        return type;
    }

    internal unsafe void WriteTo(ref MessageWriter writer)
    {
        switch (Type)
        {
            case DBusType.Byte:
                writer.WriteVariantByte(GetByte());
                break;
            case DBusType.Bool:
                writer.WriteVariantBool(GetBool());
                break;
            case DBusType.Int16:
                writer.WriteVariantInt16(GetInt16());
                break;
            case DBusType.UInt16:
                writer.WriteVariantUInt16(GetUInt16());
                break;
            case DBusType.Int32:
                writer.WriteVariantInt32(GetInt32());
                break;
            case DBusType.UInt32:
                writer.WriteVariantUInt32(GetUInt32());
                break;
            case DBusType.Int64:
                writer.WriteVariantInt64(GetInt64());
                break;
            case DBusType.UInt64:
                writer.WriteVariantUInt64(GetUInt64());
                break;
            case DBusType.Double:
                writer.WriteVariantDouble(GetDouble());
                break;
            case DBusType.String:
                writer.WriteVariantString(GetString());
                break;
            case DBusType.ObjectPath:
                writer.WriteVariantObjectPath(GetObjectPath());
                break;
            case DBusType.Signature:
                writer.WriteVariantSignature(GetSignature());
                break;
            case DBusType.UnixFd:
                writer.WriteVariantHandle(GetUnixFd());
                break;

            case DBusType.Array:
            case DBusType.Struct:
                Utf8Span signature;
                IDBusWritable writable;
                if ((_l << 8) == 0)
                {
                    // The signature is stored in the object.
                    var o = (ValueTuple<byte[], IDBusWritable>)_o!;
                    signature = new Utf8Span(o.Item1);
                    writable = o.Item2;
                }
                else
                {
                    // The signature is stored in _l.
                    long l = _l;
                    if (BitConverter.IsLittleEndian)
                    {
                        l = BinaryPrimitives.ReverseEndianness(l);
                    }
                    Span<byte> span = new Span<byte>(&l, 8);
                    int length = span.IndexOf((byte)0);
                    if (length == -1)
                    {
                        length = 8;
                    }
                    signature = new Utf8Span(span.Slice(0, length));
                    writable = (_o as IDBusWritable)!;
                }
                writer.WriteSignature(signature);
                writable.WriteTo(ref writer);
                break;
            default:
                throw new InvalidOperationException($"Cannot write Variant of type {Type}.");
        }
    }
}
