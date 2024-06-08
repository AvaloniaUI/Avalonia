namespace Tmds.DBus.Protocol;

public readonly struct VariantValue : IEquatable<VariantValue>
{
    private static readonly object Int64Type = VariantValueType.Int64;
    private static readonly object UInt64Type = VariantValueType.UInt64;
    private static readonly object DoubleType = VariantValueType.Double;
    private readonly object? _o;
    private readonly long    _l;

    private const int TypeShift = 8 * 7;
    private const int ArrayItemTypeShift = 8 * 0;
    private const int DictionaryKeyTypeShift = 8 * 0;
    private const int DictionaryValueTypeShift = 8 * 1;
    private const long StripTypeMask = ~(0xffL << TypeShift);

    private const long ArrayOfByte = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Byte << ArrayItemTypeShift);
    private const long ArrayOfInt16 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Int16 << ArrayItemTypeShift);
    private const long ArrayOfUInt16 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.UInt16 << ArrayItemTypeShift);
    private const long ArrayOfInt32 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Int32 << ArrayItemTypeShift);
    private const long ArrayOfUInt32 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.UInt32 << ArrayItemTypeShift);
    private const long ArrayOfInt64 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Int64 << ArrayItemTypeShift);
    private const long ArrayOfUInt64 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.UInt64 << ArrayItemTypeShift);
    private const long ArrayOfDouble = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Double << ArrayItemTypeShift);
    private const long ArrayOfString = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.String << ArrayItemTypeShift);
    private const long ArrayOfObjectPath = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.ObjectPath << ArrayItemTypeShift);

    public VariantValueType Type
        => DetermineType();

    internal VariantValue(byte value)
    {
        _l = value | ((long)VariantValueType.Byte << TypeShift);
        _o = null;
    }
    internal VariantValue(bool value)
    {
        _l = (value ? 1L : 0) | ((long)VariantValueType.Bool << TypeShift);
        _o = null;
    }
    internal VariantValue(short value)
    {
        _l = (ushort)value | ((long)VariantValueType.Int16 << TypeShift);
        _o = null;
    }
    internal VariantValue(ushort value)
    {
        _l = value | ((long)VariantValueType.UInt16 << TypeShift);
        _o = null;
    }
    internal VariantValue(int value)
    {
        _l = (uint)value | ((long)VariantValueType.Int32 << TypeShift);
        _o = null;
    }
    internal VariantValue(uint value)
    {
        _l = value | ((long)VariantValueType.UInt32 << TypeShift);
        _o = null;
    }
    internal VariantValue(long value)
    {
        _l = value;
        _o = Int64Type;
    }
    internal VariantValue(ulong value)
    {
        _l = (long)value;
        _o = UInt64Type;
    }
    internal unsafe VariantValue(double value)
    {
        _l = *(long*)&value;
        _o = DoubleType;
    }
    internal VariantValue(string value)
    {
        _l = (long)VariantValueType.String << TypeShift;
        _o = value ?? throw new ArgumentNullException(nameof(value));
    }
    internal VariantValue(ObjectPath value)
    {
        _l = (long)VariantValueType.ObjectPath << TypeShift;
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    internal VariantValue(Signature value)
    {
        _l = (long)VariantValueType.Signature << TypeShift;
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    // Array
    internal VariantValue(VariantValueType itemType, VariantValue[] items)
    {
        Debug.Assert(
            itemType != VariantValueType.Byte &&
            itemType != VariantValueType.Int16 &&
            itemType != VariantValueType.UInt16 &&
            itemType != VariantValueType.Int32 &&
            itemType != VariantValueType.UInt32 &&
            itemType != VariantValueType.Int64 &&
            itemType != VariantValueType.UInt64 &&
            itemType != VariantValueType.Double
        );
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    internal VariantValue(VariantValueType itemType, string[] items)
    {
        Debug.Assert(itemType == VariantValueType.String || itemType == VariantValueType.ObjectPath);
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    internal VariantValue(byte[] items)
    {
        _l = ArrayOfByte;
        _o = items;
    }
    internal VariantValue(short[] items)
    {
        _l = ArrayOfInt16;
        _o = items;
    }
    internal VariantValue(ushort[] items)
    {
        _l = ArrayOfUInt16;
        _o = items;
    }
    internal VariantValue(int[] items)
    {
        _l = ArrayOfInt32;
        _o = items;
    }
    internal VariantValue(uint[] items)
    {
        _l = ArrayOfUInt32;
        _o = items;
    }
    internal VariantValue(long[] items)
    {
        _l = ArrayOfInt64;
        _o = items;
    }
    internal VariantValue(ulong[] items)
    {
        _l = ArrayOfUInt64;
        _o = items;
    }
    internal VariantValue(double[] items)
    {
        _l = ArrayOfDouble;
        _o = items;
    }
    // Dictionary
    internal VariantValue(VariantValueType keyType, VariantValueType valueType, KeyValuePair<VariantValue, VariantValue>[] pairs)
    {
        _l = ((long)VariantValueType.Dictionary << TypeShift) |
             ((long)keyType << DictionaryKeyTypeShift) |
             ((long)valueType << DictionaryValueTypeShift);
        _o = pairs;
    }
    // Struct
    internal VariantValue(VariantValue[] fields)
    {
        _l = ((long)VariantValueType.Struct << TypeShift);
        _o = fields;
    }
    // UnixFd
    internal VariantValue(UnixFdCollection? fdCollection, int index)
    {
        _l = (long)index | ((long)VariantValueType.UnixFd << TypeShift);
        _o = fdCollection;
    }

    public byte GetByte()
    {
        EnsureTypeIs(VariantValueType.Byte);
        return UnsafeGetByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte UnsafeGetByte()
    {
        return (byte)(_l & StripTypeMask);
    }

    public bool GetBool()
    {
        EnsureTypeIs(VariantValueType.Bool);
        return UnsafeGetBool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool UnsafeGetBool()
    {
        return (_l & StripTypeMask) != 0;
    }

    public short GetInt16()
    {
        EnsureTypeIs(VariantValueType.Int16);
        return UnsafeGetInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private short UnsafeGetInt16()
    {
        return (short)(_l & StripTypeMask);
    }

    public ushort GetUInt16()
    {
        EnsureTypeIs(VariantValueType.UInt16);
        return UnsafeGetUInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort UnsafeGetUInt16()
    {
        return (ushort)(_l & StripTypeMask);
    }

    public int GetInt32()
    {
        EnsureTypeIs(VariantValueType.Int32);
        return UnsafeGetInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UnsafeGetInt32()
    {
        return (int)(_l & StripTypeMask);
    }

    public uint GetUInt32()
    {
        EnsureTypeIs(VariantValueType.UInt32);
        return UnsafeGetUInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint UnsafeGetUInt32()
    {
        return (uint)(_l & StripTypeMask);
    }

    public long GetInt64()
    {
        EnsureTypeIs(VariantValueType.Int64);
        return UnsafeGetInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long UnsafeGetInt64()
    {
        return _l;
    }

    public ulong GetUInt64()
    {
        EnsureTypeIs(VariantValueType.UInt64);
        return UnsafeGetUInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong UnsafeGetUInt64()
    {
        return (ulong)(_l);
    }

    public string GetString()
    {
        EnsureTypeIs(VariantValueType.String);
        return UnsafeGetString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string UnsafeGetString()
    {
        return (_o as string)!;
    }

    public string GetObjectPath()
    {
        EnsureTypeIs(VariantValueType.ObjectPath);
        return UnsafeGetString();
    }

    public string GetSignature()
    {
        EnsureTypeIs(VariantValueType.Signature);
        return UnsafeGetString();
    }

    public double GetDouble()
    {
        EnsureTypeIs(VariantValueType.Double);
        return UnsafeGetDouble();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe double UnsafeGetDouble()
    {
        double value;
        *(long*)&value = _l;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T UnsafeGet<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)UnsafeGetByte();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)UnsafeGetBool();
        }
        else if (typeof(T) == typeof(short))
        {
            return (T)(object)UnsafeGetInt16();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T)(object)UnsafeGetUInt16();
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)UnsafeGetInt32();
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T)(object)UnsafeGetUInt32();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T)(object)UnsafeGetInt64();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T)(object)UnsafeGetUInt64();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)UnsafeGetDouble();
        }
        else if (typeof(T) == typeof(string))
        {
            return (T)(object)UnsafeGetString();
        }
        else if (typeof(T) == typeof(VariantValue))
        {
            return (T)(object)this;
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            return (T)(object)UnsafeReadHandle<T>()!;
        }

        ThrowCannotRetrieveAs(Type, typeof(T));
        return default!;
    }

    public Dictionary<TKey, TValue> GetDictionary
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
        >
        ()
        where TKey : notnull
        where TValue : notnull
    {
        EnsureTypeIs(VariantValueType.Dictionary);
        EnsureCanUnsafeGet<TKey>(KeyType);
        EnsureCanUnsafeGet<TValue>(ValueType);

        Dictionary<TKey, TValue> dict = new();
        var pairs = (_o as KeyValuePair<VariantValue, VariantValue>[])!.AsSpan();
        foreach (var pair in pairs)
        {
            dict[pair.Key.UnsafeGet<TKey>()] = pair.Value.UnsafeGet<TValue>();
        }
        return dict;
    }

    public T[] GetArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
        where T : notnull
    {
        EnsureTypeIs(VariantValueType.Array);
        EnsureCanUnsafeGet<T>(ItemType);

        // Return the array by reference when we can.
        // Don't bother to make a copy in case the caller mutates the data and
        // calls GetArray again to retrieve the original data. It's an unlikely scenario.
        if (typeof(T) == typeof(byte))
        {
            return (T[])(object)(_o as byte[])!;
        }
        else if (typeof(T) == typeof(short))
        {
            return (T[])(object)(_o as short[])!;
        }
        else if (typeof(T) == typeof(int))
        {
            return (T[])(object)(_o as int[])!;
        }
        else if (typeof(T) == typeof(long))
        {
            return (T[])(object)(_o as long[])!;
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T[])(object)(_o as ushort[])!;
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T[])(object)(_o as uint[])!;
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T[])(object)(_o as ulong[])!;
        }
        else if (typeof(T) == typeof(double))
        {
            return (T[])(object)(_o as double[])!;
        }
        else if (typeof(T) == typeof(string))
        {
            return (T[])(object)(_o as string[])!;
        }
        else
        {
            var items = (_o as VariantValue[])!.AsSpan();
            T[] array = new T[items.Length];
            int i = 0;
            foreach (var item in items)
            {
                array[i++] = item.UnsafeGet<T>();
            }
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCanUnsafeGet<T>(VariantValueType type)
    {
        if (typeof(T) == typeof(byte))
        {
            EnsureTypeIs(type, VariantValueType.Byte);
        }
        else if (typeof(T) == typeof(bool))
        {
            EnsureTypeIs(type, VariantValueType.Bool);
        }
        else if (typeof(T) == typeof(short))
        {
            EnsureTypeIs(type, VariantValueType.Int16);
        }
        else if (typeof(T) == typeof(ushort))
        {
            EnsureTypeIs(type, VariantValueType.UInt16);
        }
        else if (typeof(T) == typeof(int))
        {
            EnsureTypeIs(type, VariantValueType.Int32);
        }
        else if (typeof(T) == typeof(uint))
        {
            EnsureTypeIs(type, VariantValueType.UInt32);
        }
        else if (typeof(T) == typeof(long))
        {
            EnsureTypeIs(type, VariantValueType.Int64);
        }
        else if (typeof(T) == typeof(ulong))
        {
            EnsureTypeIs(type, VariantValueType.UInt64);
        }
        else if (typeof(T) == typeof(double))
        {
            EnsureTypeIs(type, VariantValueType.Double);
        }
        else if (typeof(T) == typeof(string))
        {
            EnsureTypeIs(type, [ VariantValueType.String, VariantValueType.Signature, VariantValueType.ObjectPath ]);
        }
        else if (typeof(T) == typeof(VariantValue))
        { }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            EnsureTypeIs(type, VariantValueType.UnixFd);
        }
        else
        {
            ThrowCannotRetrieveAs(type, typeof(T));
        }
    }

    public T? ReadHandle<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    T>() where T : SafeHandle
    {
        EnsureTypeIs(VariantValueType.UnixFd);
        return UnsafeReadHandle<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T? UnsafeReadHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        var handles = (UnixFdCollection?)_o;
        if (handles is null)
        {
            return default;
        }
        int index = (int)_l;
        return handles.ReadHandleGeneric<T>(index);
    }

    // Use for Array, Struct and Dictionary.
    public int Count
    {
        get
        {
            Array? array = _o as Array;
            return array?.Length ?? -1;
        }
    }

    // Valid for Array, Struct.
    public VariantValue GetItem(int i)
    {
        if (Type == VariantValueType.Array)
        {
            switch (_l)
            {
                case ArrayOfByte:
                    return new VariantValue((_o as byte[])![i]);
                case ArrayOfInt16:
                    return new VariantValue((_o as short[])![i]);
                case ArrayOfUInt16:
                    return new VariantValue((_o as ushort[])![i]);
                case ArrayOfInt32:
                    return new VariantValue((_o as int[])![i]);
                case ArrayOfUInt32:
                    return new VariantValue((_o as uint[])![i]);
                case ArrayOfInt64:
                    return new VariantValue((_o as long[])![i]);
                case ArrayOfUInt64:
                    return new VariantValue((_o as ulong[])![i]);
                case ArrayOfDouble:
                    return new VariantValue((_o as double[])![i]);
                case ArrayOfString:
                case ArrayOfObjectPath:
                    return new VariantValue((_o as string[])![i]);
            }
        }
        var values = _o as VariantValue[];
        if (_o is null)
        {
            ThrowCannotRetrieveAs(Type, [VariantValueType.Array, VariantValueType.Struct]);
        }
        return values![i];
    }

    // Valid for Dictionary.
    public KeyValuePair<VariantValue, VariantValue> GetDictionaryEntry(int i)
    {
        var values = _o as KeyValuePair<VariantValue, VariantValue>[];
        if (_o is null)
        {
            ThrowCannotRetrieveAs(Type, VariantValueType.Dictionary);
        }
        return values![i];
    }

    // implicit conversion to VariantValue for basic D-Bus types (except Unix_FD).
    public static implicit operator VariantValue(byte value)
        => new VariantValue(value);
    public static implicit operator VariantValue(bool value)
        => new VariantValue(value);
    public static implicit operator VariantValue(short value)
        => new VariantValue(value);
    public static implicit operator VariantValue(ushort value)
        => new VariantValue(value);
    public static implicit operator VariantValue(int value)
        => new VariantValue(value);
    public static implicit operator VariantValue(uint value)
        => new VariantValue(value);
    public static implicit operator VariantValue(long value)
        => new VariantValue(value);
    public static implicit operator VariantValue(ulong value)
        => new VariantValue(value);
    public static implicit operator VariantValue(double value)
        => new VariantValue(value);
    public static implicit operator VariantValue(string value)
        => new VariantValue(value);
    public static implicit operator VariantValue(ObjectPath value)
        => new VariantValue(value);
    public static implicit operator VariantValue(Signature value)
        => new VariantValue(value);

    public VariantValueType ItemType
        => DetermineInnerType(VariantValueType.Array, ArrayItemTypeShift);

    public VariantValueType KeyType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryKeyTypeShift);

    public VariantValueType ValueType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryValueTypeShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTypeIs(VariantValueType expected)
        => EnsureTypeIs(Type, expected);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureTypeIs(VariantValueType actual, VariantValueType expected)
    {
        if (actual != expected)
        {
            ThrowCannotRetrieveAs(actual, expected);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureTypeIs(VariantValueType actual, VariantValueType[] expected)
    {
        if (Array.IndexOf<VariantValueType>(expected, actual) == -1)
        {
            ThrowCannotRetrieveAs(actual, expected);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValueType DetermineInnerType(VariantValueType outer, int typeShift)
    {
        VariantValueType type = DetermineType();
        return type == outer ? (VariantValueType)((_l >> typeShift) & 0xff) : VariantValueType.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValueType DetermineType()
    {
        // For most types, we store the VariantValueType in the highest byte of the long.
        // Except for some types, like Int64, for which we store the value allocation free
        // in the long, and use the object field to store the type.
        VariantValueType type = (VariantValueType)(_l >> TypeShift);
        if (_o is not null)
        {
            if (_o.GetType() == typeof(VariantValueType))
            {
                type = (VariantValueType)_o;
            }
        }
        return type;
    }

    private static void ThrowCannotRetrieveAs(VariantValueType from, VariantValueType to)
        => ThrowCannotRetrieveAs(from.ToString(), [ to.ToString() ]);

    private static void ThrowCannotRetrieveAs(VariantValueType from, VariantValueType[] to)
        => ThrowCannotRetrieveAs(from.ToString(), to.Select(expected => expected.ToString()));

    private static void ThrowCannotRetrieveAs(string from, string to)
        => ThrowCannotRetrieveAs(from, [ to ]);

    private static void ThrowCannotRetrieveAs(VariantValueType from, Type to)
        => ThrowCannotRetrieveAs(from.ToString(), to.FullName ?? "?<Type>?");

    private static void ThrowCannotRetrieveAs(string from, IEnumerable<string> to)
    {
        throw new InvalidOperationException($"Type {from} can not be retrieved as {string.Join("/", to)}.");
    }

    public override string ToString()
        => ToString(includeTypeSuffix: true);

    public string ToString(bool includeTypeSuffix)
    {
        // This is implemented so something user-friendly shows in the debugger.
        // By overriding the ToString method, it will also affect generic types like KeyValueType<TKey, TValue> that call ToString.
        VariantValueType type = Type;
        switch (type)
        {
            case VariantValueType.Byte:
                return $"{GetByte()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Bool:
                return $"{GetBool()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Int16:
                return $"{GetInt16()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.UInt16:
                return $"{GetUInt16()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Int32:
                return $"{GetInt32()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.UInt32:
                return $"{GetUInt32()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Int64:
                return $"{GetInt64()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.UInt64:
                return $"{GetUInt64()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Double:
                return $"{GetDouble()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.String:
                return $"{GetString()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.ObjectPath:
                return $"{GetObjectPath()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Signature:
                return $"{GetSignature()}{TypeSuffix(includeTypeSuffix, type)}";

            case VariantValueType.Array:
                return $"[{nameof(VariantValueType.Array)}<{ItemType}>, Count={Count}]";
            case VariantValueType.Struct:
                var values = (_o as VariantValue[]) ?? Array.Empty<VariantValue>();
                return $"({
                            string.Join(", ", values.Select(v => v.ToString(includeTypeSuffix: false)))
                          }){(
                            !includeTypeSuffix ? ""
                                : $" [{nameof(VariantValueType.Struct)}]<{
                                    string.Join(", ", values.Select(v => v.Type))
                                }>]")})";
            case VariantValueType.Dictionary:
                return $"[{nameof(VariantValueType.Dictionary)}<{KeyType}, {ValueType}>, Count={Count}]";
            case VariantValueType.UnixFd:
                return $"[{nameof(VariantValueType.UnixFd)}]";

            case VariantValueType.Invalid:
                return $"[{nameof(VariantValueType.Invalid)}]";
            case VariantValueType.VariantValue: // note: No VariantValue returns this as its Type.
            default:
                return $"[?{Type}?]";
        }
    }

    static string TypeSuffix(bool includeTypeSuffix, VariantValueType type)
        => includeTypeSuffix ? $" [{type}]" : "";

    public static bool operator==(VariantValue lhs, VariantValue rhs)
        => lhs.Equals(rhs);

    public static bool operator!=(VariantValue lhs, VariantValue rhs)
        => !lhs.Equals(rhs);

    public override bool Equals(object? obj)
    {
        if (obj is not null && obj.GetType() == typeof(VariantValue))
        {
            return ((VariantValue)obj).Equals(this);
        }
        return false;
    }

    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        return _l.GetHashCode() + 17 * (_o?.GetHashCode() ?? 0);
#else
        return HashCode.Combine(_l, _o);
#endif
    }

    public bool Equals(VariantValue other)
    {
        if (_l == other._l && object.ReferenceEquals(_o, other._o))
        {
            return true;
        }
        VariantValueType type = Type;
        if (type != other.Type)
        {
            return false;
        }
        switch (type)
        {
            case VariantValueType.String:
            case VariantValueType.ObjectPath:
            case VariantValueType.Signature:
                return (_o as string)!.Equals(other._o as string, StringComparison.Ordinal);
        }
        // Always return false for composite types and handles.
        return false;
    }
}