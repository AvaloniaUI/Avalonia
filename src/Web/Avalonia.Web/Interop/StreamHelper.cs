using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Web.Storage;

/// <summary>
/// Set of FileSystemWritableFileStream and Blob methods.
/// </summary>
internal static partial class StreamHelper
{
    [JSImport("StreamHelper.seek", "avalonia.ts")]
    public static partial void Seek(JSObject stream, [JSMarshalAs<JSType.Number>] long position);

    [JSImport("StreamHelper.truncate", "avalonia.ts")]
    public static partial void Truncate(JSObject stream, [JSMarshalAs<JSType.Number>] long position);

    [JSImport("StreamHelper.write", "avalonia.ts")]
    public static partial void Write(JSObject stream, [JSMarshalAs<JSType.MemoryView>] Span<byte> data);

    [JSImport("StreamHelper.write", "avalonia.ts")]
    public static partial Task WriteAsync(JSObject stream, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> data);

    [JSImport("StreamHelper.close", "avalonia.ts")]
    public static partial void Close(JSObject stream);

    [JSImport("StreamHelper.close", "avalonia.ts")]
    public static partial Task CloseAsync(JSObject stream);

    [JSImport("StreamHelper.size", "avalonia.ts")]
    [return: JSMarshalAs<JSType.Number>]
    public static partial long Size(JSObject stream);

    [JSImport("StreamHelper.byteLength", "avalonia.ts")]
    [return: JSMarshalAs<JSType.Number>]
    public static partial long ByteLength(JSObject stream);

    [JSImport("StreamHelper.sliceToArray", "avalonia.ts")]
    [return: JSMarshalAs<JSType.MemoryView>]
    public static partial Span<byte> Slice(JSObject stream, [JSMarshalAs<JSType.Number>] long offset, int count);

    public static async Task<ArraySegment<byte>> SliceAsync(JSObject stream, long offset, int count)
    {
        using var buffer = await SliceToBufferAsync(stream, offset, count);
        return BufferToArray(buffer);
    }

    [JSImport("StreamHelper.slice", "avalonia.ts")]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    private static partial Task<JSObject> SliceToBufferAsync(JSObject stream, [JSMarshalAs<JSType.Number>] long offset, int count);

    [JSImport("StreamHelper.toArray", "avalonia.ts")]
    [return: JSMarshalAs<JSType.MemoryView>]
    private static partial ArraySegment<byte> BufferToArray(JSObject stream);
}
