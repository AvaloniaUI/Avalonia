using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Web.Storage;

/// <summary>
/// Set of FileSystemWritableFileStream and Blob methods.
/// </summary>
internal static partial class StreamHelper
{
    [JSImport("StreamHelper.seek", "avalonia")]
    public static partial void Seek(JSObject stream, [JSMarshalAs<JSType.Number>] long position);

    [JSImport("StreamHelper.truncate", "avalonia")]
    public static partial void Truncate(JSObject stream, [JSMarshalAs<JSType.Number>] long size);

    [JSImport("StreamHelper.write", "avalonia")]
    public static partial Task WriteAsync(JSObject stream, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> data);

    [JSImport("StreamHelper.close", "avalonia")]
    public static partial Task CloseAsync(JSObject stream);

    [JSImport("StreamHelper.byteLength", "avalonia")]
    [return: JSMarshalAs<JSType.Number>]
    public static partial long ByteLength(JSObject stream);

    [JSImport("StreamHelper.sliceArrayBuffer", "avalonia")]
    private static partial Task<JSObject> SliceToArrayBuffer(JSObject stream, [JSMarshalAs<JSType.Number>] long offset, int count);

    [JSImport("StreamHelper.toMemoryView", "avalonia")]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    private static partial byte[] ArrayBufferToMemoryView(JSObject stream);

    public static async Task<byte[]> SliceAsync(JSObject stream, long offset, int count)
    {
        using var buffer = await SliceToArrayBuffer(stream, offset, count);
        return ArrayBufferToMemoryView(buffer);
    }
}
