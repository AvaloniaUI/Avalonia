namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public byte[] ReadArrayOfByte()
        => ReadArrayOfNumeric<byte>();

    public bool[] ReadArrayOfBool()
        => ReadArrayOfT<bool>();

    public short[] ReadArrayOfInt16()
        => ReadArrayOfNumeric<short>();

    public ushort[] ReadArrayOfUInt16()
        => ReadArrayOfNumeric<ushort>();

    public int[] ReadArrayOfInt32()
        => ReadArrayOfNumeric<int>();

    public uint[] ReadArrayOfUInt32()
        => ReadArrayOfNumeric<uint>();

    public long[] ReadArrayOfInt64()
        => ReadArrayOfNumeric<long>();

    public ulong[] ReadArrayOfUInt64()
        => ReadArrayOfNumeric<ulong>();

    public double[] ReadArrayOfDouble()
        => ReadArrayOfNumeric<double>();

    public string[] ReadArrayOfString()
        => ReadArrayOfT<string>();

    public ObjectPath[] ReadArrayOfObjectPath()
        => ReadArrayOfT<ObjectPath>();

    public Signature[] ReadArrayOfSignature()
        => ReadArrayOfT<Signature>();

    public VariantValue[] ReadArrayOfVariantValue()
        => ReadArrayOfT<VariantValue>();

    public T[] ReadArrayOfHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle
        => ReadArrayOfT<T>();

    [RequiresUnreferencedCode(Strings.UseNonGenericReadArray)]
    [Obsolete(Strings.UseNonGenericReadArrayObsolete)]
    public T[] ReadArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        if (typeof(T) == typeof(byte))
        {
            return (T[])(object)ReadArrayOfNumeric<byte>();
        }
        else if (typeof(T) == typeof(short))
        {
            return (T[])(object)ReadArrayOfNumeric<short>();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T[])(object)ReadArrayOfNumeric<ushort>();
        }
        else if (typeof(T) == typeof(int))
        {
            return (T[])(object)ReadArrayOfNumeric<int>();
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T[])(object)ReadArrayOfNumeric<uint>();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T[])(object)ReadArrayOfNumeric<long>();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T[])(object)ReadArrayOfNumeric<ulong>();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T[])(object)ReadArrayOfNumeric<double>();
        }
        else
        {
            return ReadArrayOfT<T>();
        }
    }

    private T[] ReadArrayOfT<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        List<T> items = new();
        ArrayEnd arrayEnd = ReadArrayStart(TypeModel.GetTypeAlignment<T>());
        while (HasNext(arrayEnd))
        {
            items.Add(Read<T>());
        }
        return items.ToArray();
    }

    private unsafe T[] ReadArrayOfNumeric<T>() where T : unmanaged
    {
        int length = ReadInt32();
        if (sizeof(T) > 4)
        {
            AlignReader(sizeof(T));
        }
        T[] array = new T[length / sizeof(T)];
        bool dataRead = _reader.TryCopyTo(MemoryMarshal.AsBytes(array.AsSpan()));
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        _reader.Advance(sizeof(T) * array.Length);
        if (sizeof(T) > 1 && ReverseEndianness)
        {
#if NET8_0_OR_GREATER
            if (sizeof(T) == 2)
            {
                var span = MemoryMarshal.Cast<T, short>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (sizeof(T) == 4)
            {
                var span = MemoryMarshal.Cast<T, int>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (sizeof(T) == 8)
            {
                Span<long> span = MemoryMarshal.Cast<T, long>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
#else
            Span<T> span = array.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (sizeof(T) == 2)
                {
                    span[i] = (T)(object)BinaryPrimitives.ReverseEndianness((short)(object)span[i]);
                }
                else if (sizeof(T) == 4)
                {
                    span[i] = (T)(object)BinaryPrimitives.ReverseEndianness((int)(object)span[i]);
                }
                else if (typeof(T) == typeof(double))
                {
                    span[i] = (T)(object)ReverseDoubleEndianness((double)(object)span[i]);
                }
                else if (sizeof(T) == 8)
                {
                    span[i] = (T)(object)BinaryPrimitives.ReverseEndianness((long)(object)span[i]);
                }
            }
#endif
        }
        return array;

#if !NET8_0_OR_GREATER
        static double ReverseDoubleEndianness(double d)
        {
            long l = *(long*)&d;
            l = BinaryPrimitives.ReverseEndianness(l);
            return *(double*)&d;
        }
#endif
    }
}
