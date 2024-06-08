using System.Net;
using System.Net.Sockets;

namespace Tmds.DBus.Protocol;

#if NETSTANDARD2_0
static partial class NetstandardExtensions
{
    public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
    {
        if (dictionary.TryGetValue(key, out value))
        {
            dictionary.Remove(key);
            return true;
        }
        return false;
    }

    public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        fixed (char* pChars = &GetNonNullPinnableReference(chars))
        fixed (byte* pBytes = &GetNonNullPinnableReference(bytes))
        {
            return encoding.GetBytes(pChars, chars.Length, pBytes, bytes.Length);
        }
    }

    public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        fixed (char* pChars = &GetNonNullPinnableReference(chars))
        fixed (byte* pBytes = &GetNonNullPinnableReference(bytes))
        {
            return encoding.GetChars(pBytes, bytes.Length, pChars, chars.Length);
        }
    }

    public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* pBytes = &GetNonNullPinnableReference(bytes))
        {
            return encoding.GetString(pBytes, bytes.Length);
        }
    }

    public static unsafe int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* pBytes = &GetNonNullPinnableReference(bytes))
        {
            return encoding.GetCharCount(pBytes, bytes.Length);
        }
    }

    public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
    {
        fixed (char* pChars = &GetNonNullPinnableReference(chars))
        {
            return encoding.GetByteCount(pChars, chars.Length);
        }
    }

    public static unsafe int GetByteCount(this Encoder encoder, ReadOnlySpan<char> chars, bool flush)
    {
        fixed (char* pChars = &GetNonNullPinnableReference(chars))
        {
            return encoder.GetByteCount(pChars, chars.Length, flush);
        }
    }

    public static unsafe void Convert(this Encoder encoder, ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
    {
        fixed (char* pChars = &GetNonNullPinnableReference(chars))
        fixed (byte* pBytes = &GetNonNullPinnableReference(bytes))
        {
            encoder.Convert(pChars, chars.Length, pBytes, bytes.Length, flush, out charsUsed, out bytesUsed, out completed);
        }
    }

    public static unsafe void Append(this StringBuilder sb, ReadOnlySpan<char> value)
    {
        fixed (char* ptr = value)
        {
            sb.Append(ptr, value.Length);
        }
    }

    public static unsafe string AsString(this ReadOnlySpan<char> chars)
    {
        fixed (char* ptr = chars)
        {
            return new string(ptr, 0, chars.Length);
        }
    }

    public static unsafe string AsString(this Span<char> chars)
        => AsString((ReadOnlySpan<char>)chars);

    public static async ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags)
    {
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            return await SocketTaskExtensions.ReceiveAsync(socket, segment, socketFlags).ConfigureAwait(false);

        throw new NotSupportedException();
    }

    public static async ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var segment))
            return await SocketTaskExtensions.SendAsync(socket, segment, socketFlags).ConfigureAwait(false);

        throw new NotSupportedException();
    }

    /// <summary>
    /// Returns a reference to the 0th element of the Span. If the Span is empty, returns a reference to fake non-null pointer. Such a reference can be used
    /// for pinning but must never be dereferenced. This is useful for interop with methods that do not accept null pointers for zero-sized buffers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ref T GetNonNullPinnableReference<T>(Span<T> span) => ref (span.Length != 0) ? ref MemoryMarshal.GetReference(span) : ref Unsafe.AsRef<T>((void*)1);

    /// <summary>
    /// Returns a reference to the 0th element of the ReadOnlySpan. If the ReadOnlySpan is empty, returns a reference to fake non-null pointer. Such a reference
    /// can be used for pinning but must never be dereferenced. This is useful for interop with methods that do not accept null pointers for zero-sized buffers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ref T GetNonNullPinnableReference<T>(ReadOnlySpan<T> span) => ref (span.Length != 0) ? ref MemoryMarshal.GetReference(span) : ref Unsafe.AsRef<T>((void*)1);
}

internal sealed class UnixDomainSocketEndPoint : EndPoint
{
    private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

    private static readonly Encoding s_pathEncoding = Encoding.UTF8;
    private const int s_nativePathOffset = 2;

    private readonly string _path;
    private readonly byte[] _encodedPath;

    public UnixDomainSocketEndPoint(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        _path = path;
        _encodedPath = s_pathEncoding.GetBytes(_path);

        if (path.Length == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(path), path,
                string.Format("The path '{0}' is of an invalid length for use with domain sockets on this platform. The length must be at least 1 characters.", path));
        }
    }

    internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
    {
        if (socketAddress == null)
        {
            throw new ArgumentNullException(nameof(socketAddress));
        }

        if (socketAddress.Family != EndPointAddressFamily)
        {
            throw new ArgumentOutOfRangeException(nameof(socketAddress));
        }

        if (socketAddress.Size > s_nativePathOffset)
        {
            _encodedPath = new byte[socketAddress.Size - s_nativePathOffset];
            for (int i = 0; i < _encodedPath.Length; i++)
            {
                _encodedPath[i] = socketAddress[s_nativePathOffset + i];
            }

            _path = s_pathEncoding.GetString(_encodedPath, 0, _encodedPath.Length);
        }
        else
        {
            _encodedPath = Array.Empty<byte>();
            _path = string.Empty;
        }
    }

    public override SocketAddress Serialize()
    {
        var result = new SocketAddress(AddressFamily.Unix, _encodedPath.Length + s_nativePathOffset);

        for (int index = 0; index < _encodedPath.Length; index++)
        {
            result[s_nativePathOffset + index] = _encodedPath[index];
        }

        return result;
    }

    public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);

    public override AddressFamily AddressFamily => EndPointAddressFamily;

    public string Path => _path;

    public override string ToString() => _path;
}
#else
static partial class NetstandardExtensions
{
    public static string AsString(this ReadOnlySpan<char> chars)
        => new string(chars);

    public static unsafe string AsString(this Span<char> chars)
        => AsString((ReadOnlySpan<char>)chars);
}
#endif
