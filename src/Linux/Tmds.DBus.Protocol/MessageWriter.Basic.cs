namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    private const int MaxSizeHint = 4096;

    public void WriteBool(bool value) => WriteUInt32(value ? 1u : 0u);

    public void WriteByte(byte value) => WritePrimitiveCore<byte>(value, DBusType.Byte);

    public void WriteInt16(short value) => WritePrimitiveCore<Int16>(value, DBusType.Int16);

    public void WriteUInt16(ushort value) => WritePrimitiveCore<UInt16>(value, DBusType.UInt16);

    public void WriteInt32(int value) => WritePrimitiveCore<Int32>(value, DBusType.Int32);

    public void WriteUInt32(uint value) => WritePrimitiveCore<UInt32>(value, DBusType.UInt32);

    public void WriteInt64(long value) => WritePrimitiveCore<Int64>(value, DBusType.Int64);

    public void WriteUInt64(ulong value) => WritePrimitiveCore<UInt64>(value, DBusType.UInt64);

    public void WriteDouble(double value) => WritePrimitiveCore<double>(value, DBusType.Double);

    public void WriteString(Utf8Span value) => WriteStringCore(value);

    public void WriteString(string value) => WriteStringCore(value);

    public void WriteSignature(Utf8Span value)
    {
        ReadOnlySpan<byte> span = value;
        int length = span.Length;
        WriteByte((byte)length);
        var dst = GetSpan(length);
        span.CopyTo(dst);
        Advance(length);
        WriteByte((byte)0);
    }

    public void WriteSignature(string s)
    {
        Span<byte> lengthSpan = GetSpan(1);
        Advance(1);
        int bytesWritten = WriteRaw(s);
        lengthSpan[0] = (byte)bytesWritten;
        WriteByte(0);
    }

    public void WriteObjectPath(Utf8Span value) => WriteStringCore(value);

    public void WriteObjectPath(string value) => WriteStringCore(value);

    public void WriteVariantBool(bool value)
    {
        WriteSignature(ProtocolConstants.BooleanSignature);
        WriteBool(value);
    }

    public void WriteVariantByte(byte value)
    {
        WriteSignature(ProtocolConstants.ByteSignature);
        WriteByte(value);
    }

    public void WriteVariantInt16(short value)
    {
        WriteSignature(ProtocolConstants.Int16Signature);
        WriteInt16(value);
    }

    public void WriteVariantUInt16(ushort value)
    {
        WriteSignature(ProtocolConstants.UInt16Signature);
        WriteUInt16(value);
    }

    public void WriteVariantInt32(int value)
    {
        WriteSignature(ProtocolConstants.Int32Signature);
        WriteInt32(value);
    }

    public void WriteVariantUInt32(uint value)
    {
        WriteSignature(ProtocolConstants.UInt32Signature);
        WriteUInt32(value);
    }

    public void WriteVariantInt64(long value)
    {
        WriteSignature(ProtocolConstants.Int64Signature);
        WriteInt64(value);
    }

    public void WriteVariantUInt64(ulong value)
    {
        WriteSignature(ProtocolConstants.UInt64Signature);
        WriteUInt64(value);
    }

    public void WriteVariantDouble(double value)
    {
        WriteSignature(ProtocolConstants.DoubleSignature);
        WriteDouble(value);
    }

    public void WriteVariantString(Utf8Span value)
    {
        WriteSignature(ProtocolConstants.StringSignature);
        WriteString(value);
    }

    public void WriteVariantSignature(Utf8Span value)
    {
        WriteSignature(ProtocolConstants.SignatureSignature);
        WriteSignature(value);
    }

    public void WriteVariantObjectPath(Utf8Span value)
    {
        WriteSignature(ProtocolConstants.ObjectPathSignature);
        WriteObjectPath(value);
    }

    public void WriteVariantString(string value)
    {
        WriteSignature(ProtocolConstants.StringSignature);
        WriteString(value);
    }

    public void WriteVariantSignature(string value)
    {
        WriteSignature(ProtocolConstants.SignatureSignature);
        WriteSignature(value);
    }

    public void WriteVariantObjectPath(string value)
    {
        WriteSignature(ProtocolConstants.ObjectPathSignature);
        WriteObjectPath(value);
    }

    private void WriteStringCore(ReadOnlySpan<byte> span)
    {
        int length = span.Length;
        WriteUInt32((uint)length);
        var dst = GetSpan(length);
        span.CopyTo(dst);
        Advance(length);
        WriteByte((byte)0);
    }

    private void WriteStringCore(string s)
    {
        WritePadding(DBusType.UInt32);
        Span<byte> lengthSpan = GetSpan(4);
        Advance(4);
        int bytesWritten = WriteRaw(s);
        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(lengthSpan), (uint)bytesWritten);
        WriteByte(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePrimitiveCore<T>(T value, DBusType type)
    {
        WritePadding(type);
        int length = ProtocolConstants.GetFixedTypeLength(type);
        var span = GetSpan(length);
        Unsafe.WriteUnaligned<T>(ref MemoryMarshal.GetReference(span), value);
        Advance(length);
    }

    private int WriteRaw(ReadOnlySpan<byte> data)
    {
        int totalLength = data.Length;
        if (totalLength <= MaxSizeHint)
        {
            var dst = GetSpan(totalLength);
            data.CopyTo(dst);
            Advance(totalLength);
            return totalLength;
        }
        else
        {
            while (!data.IsEmpty)
            {
                var dst = GetSpan(1);
                int length = Math.Min(data.Length, dst.Length);
                data.Slice(0, length).CopyTo(dst);
                Advance(length);
                data = data.Slice(length);
            }
            return totalLength;
        }
    }

    private int WriteRaw(string data)
    {
        const int MaxUtf8BytesPerChar = 3;

        if (data.Length <= MaxSizeHint / MaxUtf8BytesPerChar)
        {
            ReadOnlySpan<char> chars = data.AsSpan();
            int byteCount = Encoding.UTF8.GetByteCount(chars);
            var dst = GetSpan(byteCount);
            byteCount = Encoding.UTF8.GetBytes(data.AsSpan(), dst);
            Advance(byteCount);
            return byteCount;
        }
        else
        {
            ReadOnlySpan<char> chars = data.AsSpan();
            Encoder encoder = Encoding.UTF8.GetEncoder();
            int totalLength = 0;
            do
            {
                Debug.Assert(!chars.IsEmpty);

                var dst = GetSpan(MaxUtf8BytesPerChar);
                encoder.Convert(chars, dst, flush: true, out int charsUsed, out int bytesUsed, out bool completed);

                Advance(bytesUsed);
                totalLength += bytesUsed;

                if (completed)
                {
                    return totalLength;
                }

                chars = chars.Slice(charsUsed);
            } while (true);
        }
    }
}
