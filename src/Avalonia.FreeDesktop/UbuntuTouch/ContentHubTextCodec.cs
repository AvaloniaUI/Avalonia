using System;
using System.Buffers.Binary;
using System.Text;

namespace Avalonia.FreeDesktop.UbuntuTouch;

// Content Hub's wire layout is a native-endian int32 table followed by MIME names
// and raw bytes. Ubuntu Touch ARM64 and our supported x64 host are little-endian.
// Protocol reference: lomiri-content-hub src/com/lomiri/content/utils.cpp.
internal static class ContentHubTextCodec
{
    internal const int MaximumBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static byte[] Encode(string text)
    {
        if (!BitConverter.IsLittleEndian) throw new PlatformNotSupportedException();
        const int headerSize = 20;
        ReadOnlySpan<byte> mime = "text/plain"u8;
        var length = Utf8.GetByteCount(text);
        if (length > MaximumBytes - headerSize - mime.Length)
            throw new ArgumentOutOfRangeException(nameof(text), "Clipboard payload exceeds the Content Hub limit.");
        var data = new byte[headerSize + mime.Length + length];
        Write(0, 1); Write(4, headerSize); Write(8, mime.Length);
        Write(12, headerSize + mime.Length); Write(16, length);
        mime.CopyTo(data.AsSpan(headerSize));
        Utf8.GetBytes(text, data.AsSpan(headerSize + mime.Length));
        return data;

        void Write(int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), value);
    }

    internal static string? Decode(ReadOnlySpan<byte> data)
    {
        if (!BitConverter.IsLittleEndian) throw new PlatformNotSupportedException();
        if (data.Length < 4 || data.Length > MaximumBytes) return null;
        var count = BinaryPrimitives.ReadInt32LittleEndian(data);
        if (count is < 0 or > 16) return null;
        var headerSize = 4 + count * 16;
        if (data.Length < headerSize) return null;
        string? text = null;
        for (var i = 0; i < count; i++)
        {
            var record = data.Slice(4 + i * 16, 16);
            var mimeOffset = BinaryPrimitives.ReadInt32LittleEndian(record);
            var mimeSize = BinaryPrimitives.ReadInt32LittleEndian(record[4..]);
            var offset = BinaryPrimitives.ReadInt32LittleEndian(record[8..]);
            var size = BinaryPrimitives.ReadInt32LittleEndian(record[12..]);
            if (!ValidRange(mimeOffset, mimeSize, headerSize, data.Length) ||
                !ValidRange(offset, size, headerSize, data.Length)) return null;
            var mime = Encoding.Latin1.GetString(data.Slice(mimeOffset, mimeSize));
            if (mime.Equals("text/plain", StringComparison.OrdinalIgnoreCase) ||
                mime.Equals("text/plain;charset=utf-8", StringComparison.OrdinalIgnoreCase) ||
                mime.Equals("text/plain; charset=utf-8", StringComparison.OrdinalIgnoreCase))
            {
                try { text ??= Utf8.GetString(data.Slice(offset, size)); }
                catch (DecoderFallbackException) { return null; }
            }
        }
        return text;
    }

    private static bool ValidRange(int offset, int size, int header, int total) =>
        offset >= header && offset <= total && size >= 0 && size <= total - offset;
}
