using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

/// <summary>
/// Set of FileSystemWritableFileStream and Blob methods.
/// </summary>
internal static partial class StreamHelper
{
    [JSImport("StreamHelper.seek", AvaloniaModule.MainModuleName)]
    public static partial void Seek(JSObject stream, [JSMarshalAs<JSType.Number>] long position);

    [JSImport("StreamHelper.truncate", AvaloniaModule.MainModuleName)]
    public static partial void Truncate(JSObject stream, [JSMarshalAs<JSType.Number>] long size);

    [JSImport("StreamHelper.write", AvaloniaModule.MainModuleName)]
    public static partial Task WriteAsync(JSObject stream, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> data, int offset, int count);

    [JSImport("StreamHelper.close", AvaloniaModule.MainModuleName)]
    public static partial Task CloseAsync(JSObject stream);

    [JSImport("StreamHelper.byteLength", AvaloniaModule.MainModuleName)]
    [return: JSMarshalAs<JSType.Number>]
    public static partial long ByteLength(JSObject stream);

    /// <summary>
    /// Reads up to <paramref name="target"/>.Count bytes of the blob starting at <paramref name="offset"/> directly into <paramref name="target"/>.
    /// </summary>
    [JSImport("StreamHelper.readInto", AvaloniaModule.MainModuleName)]
    public static partial Task<int> ReadIntoAsync(JSObject stream, [JSMarshalAs<JSType.Number>] long offset, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> target);
}
