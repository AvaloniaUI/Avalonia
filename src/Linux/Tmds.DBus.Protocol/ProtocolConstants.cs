namespace Tmds.DBus.Protocol;

static class ProtocolConstants
{
    public const int MaxSignatureLength = 256;

    // note: C# compiler treats these as static data.
    public static ReadOnlySpan<byte> ByteSignature => new byte[] { (byte)'y' };
    public static ReadOnlySpan<byte> BooleanSignature => new byte[] { (byte)'b' };
    public static ReadOnlySpan<byte> Int16Signature => new byte[] { (byte)'n' };
    public static ReadOnlySpan<byte> UInt16Signature => new byte[] { (byte)'q' };
    public static ReadOnlySpan<byte> Int32Signature => new byte[] { (byte)'i' };
    public static ReadOnlySpan<byte> UInt32Signature => new byte[] { (byte)'u' };
    public static ReadOnlySpan<byte> Int64Signature => new byte[] { (byte)'x' };
    public static ReadOnlySpan<byte> UInt64Signature => new byte[] { (byte)'t' };
    public static ReadOnlySpan<byte> DoubleSignature => new byte[] { (byte)'d' };
    public static ReadOnlySpan<byte> UnixFdSignature => new byte[] { (byte)'h' };
    public static ReadOnlySpan<byte> StringSignature => new byte[] { (byte)'s' };
    public static ReadOnlySpan<byte> ObjectPathSignature => new byte[] { (byte)'o' };
    public static ReadOnlySpan<byte> SignatureSignature => new byte[] { (byte)'g' };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetTypeAlignment(DBusType type)
    {
        switch (type)
        {
            case DBusType.Byte: return 1;
            case DBusType.Bool: return 4;
            case DBusType.Int16: return 2;
            case DBusType.UInt16: return 2;
            case DBusType.Int32: return 4;
            case DBusType.UInt32: return 4;
            case DBusType.Int64: return 8;
            case DBusType.UInt64: return 8;
            case DBusType.Double: return 8;
            case DBusType.String: return 4;
            case DBusType.ObjectPath: return 4;
            case DBusType.Signature: return 4;
            case DBusType.Array: return 4;
            case DBusType.Struct: return 8;
            case DBusType.Variant: return 1;
            case DBusType.DictEntry: return 8;
            case DBusType.UnixFd: return 4;
            default: return 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFixedTypeLength(DBusType type)
    {
        switch (type)
        {
            case DBusType.Byte: return 1;
            case DBusType.Bool: return 4;
            case DBusType.Int16: return 2;
            case DBusType.UInt16: return 2;
            case DBusType.Int32: return 4;
            case DBusType.UInt32: return 4;
            case DBusType.Int64: return 8;
            case DBusType.UInt64: return 8;
            case DBusType.Double: return 8;
            case DBusType.UnixFd: return 4;
            default: return 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Align(int offset, DBusType type)
    {
        return offset + GetPadding(offset, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPadding(int offset, DBusType type)
    {
        int alignment = GetTypeAlignment(type);
        return GetPadding(offset ,alignment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPadding(int offset, int alignment)
    {
        return (~offset + 1) & (alignment - 1);
    }
}