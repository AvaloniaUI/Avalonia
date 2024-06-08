using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Tmds.DBus.Protocol;

#if NETSTANDARD2_0 || NETSTANDARD2_1
static partial class NetstandardExtensions
{

    private static PropertyInfo s_safehandleProperty = typeof(Socket).GetTypeInfo().GetDeclaredProperty("SafeHandle");

    private const int MaxInputElementsPerIteration = 1 * 1024 * 1024;

    public static bool IsAssignableTo(this Type type, Type? targetType)
        => targetType?.IsAssignableFrom(type) ?? false;

    public static SafeHandle GetSafeHandle(this Socket socket)
    {
        if (s_safehandleProperty != null)
        {
            return (SafeHandle)s_safehandleProperty.GetValue(socket, null);
        }
        ThrowHelper.ThrowNotSupportedException();
        return null!;
    }

    public static long GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, IBufferWriter<byte> writer)
    {
        if (chars.Length <= MaxInputElementsPerIteration)
        {
            int byteCount = encoding.GetByteCount(chars);
            Span<byte> scratchBuffer = writer.GetSpan(byteCount);

            int actualBytesWritten = encoding.GetBytes(chars, scratchBuffer);

            writer.Advance(actualBytesWritten);
            return actualBytesWritten;
        }
        else
        {
            Convert(encoding.GetEncoder(), chars, writer, flush: true, out long totalBytesWritten, out bool completed);
            return totalBytesWritten;
        }
    }

    public static void Convert(this Encoder encoder, ReadOnlySpan<char> chars, IBufferWriter<byte> writer, bool flush, out long bytesUsed, out bool completed)
    {
        long totalBytesWritten = 0;
        do
        {
            int byteCountForThisSlice = (chars.Length <= MaxInputElementsPerIteration)
              ? encoder.GetByteCount(chars, flush)
              : encoder.GetByteCount(chars.Slice(0, MaxInputElementsPerIteration), flush: false);

            Span<byte> scratchBuffer = writer.GetSpan(byteCountForThisSlice);

            encoder.Convert(chars, scratchBuffer, flush, out int charsUsedJustNow, out int bytesWrittenJustNow, out completed);

            chars = chars.Slice(charsUsedJustNow);
            writer.Advance(bytesWrittenJustNow);
            totalBytesWritten += bytesWrittenJustNow;
        } while (!chars.IsEmpty);

        bytesUsed = totalBytesWritten;
    }

    public static async Task ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
    {
        using var ctr = cancellationToken.Register(state => ((Socket)state!).Dispose(), socket, useSynchronizationContext: false);
        try
        {
            await Task.Factory.FromAsync(
                (targetEndPoint, callback, state) => ((Socket)state).BeginConnect(targetEndPoint, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                remoteEP,
                state: socket).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw;
        }
    }
}
#else
static partial class NetstandardExtensions
{
    public static SafeHandle GetSafeHandle(this Socket socket)
        => socket.SafeHandle;
}
#endif
