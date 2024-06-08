namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteArray(byte[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<byte> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<byte> value)
        => WriteArrayOfT(value);

    public void WriteArray(short[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<short> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<short> value)
        => WriteArrayOfT(value);

    public void WriteArray(ushort[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<ushort> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<ushort> value)
        => WriteArrayOfT(value);

    public void WriteArray(int[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<int> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<int> value)
        => WriteArrayOfT(value);

    public void WriteArray(uint[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<uint> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<uint> value)
        => WriteArrayOfT(value);

    public void WriteArray(long[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<long> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<long> value)
        => WriteArrayOfT(value);

    public void WriteArray(ulong[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<ulong> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<ulong> value)
        => WriteArrayOfT(value);

    public void WriteArray(double[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<double> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<double> value)
        => WriteArrayOfT(value);

    public void WriteArray(string[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<string> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<string> value)
        => WriteArrayOfT(value);

    public void WriteArray(Signature[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<Signature> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<Signature> value)
        => WriteArrayOfT(value);

    public void WriteArray(ObjectPath[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<ObjectPath> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<ObjectPath> value)
        => WriteArrayOfT(value);

    public void WriteArray(Variant[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<Variant> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<Variant> value)
        => WriteArrayOfT(value);

    public void WriteArray(SafeHandle[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<SafeHandle> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<SafeHandle> value)
        => WriteArrayOfT(value);

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteArray)]
    [Obsolete(Strings.UseNonGenericWriteArrayObsolete)]
    public void WriteArray<T>(IEnumerable<T> value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    internal void WriteArray<T>(ReadOnlySpan<T> value)
        where T : notnull
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        if (typeof(T) == typeof(byte))
        {
            ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(short))
        {
            ReadOnlySpan<short> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(ushort))
        {
            ReadOnlySpan<ushort> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(int))
        {
            ReadOnlySpan<int> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(uint))
        {
            ReadOnlySpan<uint> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(long))
        {
            ReadOnlySpan<long> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(ulong))
        {
            ReadOnlySpan<ulong> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(double))
        {
            ReadOnlySpan<double> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, double>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else
#endif
        {
            WriteArrayOfT<T>(value);
        }
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteArray)]
    [Obsolete(Strings.UseNonGenericWriteArrayObsolete)]
    public void WriteArray<T>(T[] value)
        where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            WriteArray((byte[])(object)value);
        }
        else if (typeof(T) == typeof(short))
        {
            WriteArray((short[])(object)value);
        }
        else if (typeof(T) == typeof(ushort))
        {
            WriteArray((ushort[])(object)value);
        }
        else if (typeof(T) == typeof(int))
        {
            WriteArray((int[])(object)value);
        }
        else if (typeof(T) == typeof(uint))
        {
            WriteArray((uint[])(object)value);
        }
        else if (typeof(T) == typeof(long))
        {
            WriteArray((long[])(object)value);
        }
        else if (typeof(T) == typeof(ulong))
        {
            WriteArray((ulong[])(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            WriteArray((double[])(object)value);
        }
        else
        {
            WriteArrayOfT<T>(value.AsSpan());
        }
    }

    private unsafe void WriteArrayOfNumeric<T>(ReadOnlySpan<T> value) where T : unmanaged
    {
        WriteInt32(value.Length * sizeof(T));
        if (sizeof(T) > 4)
        {
            WritePadding(sizeof(T));
        }
        WriteRaw(MemoryMarshal.AsBytes(value));
    }

    private void WriteArrayOfT<T>(ReadOnlySpan<T> value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    private void WriteArrayOfT<T>(IEnumerable<T> value)
        where T : notnull
    {
        if (value is T[] array)
        {
            WriteArrayOfT<T>(array.AsSpan());
            return;
        }
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    private static void WriteArraySignature<T>(ref MessageWriter writer) where T : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Array<T>>(buffer));
    }
}
