using System;

namespace Avalonia.Wayland;

/// <summary>
/// Helpers for converting between UTF-8 byte offsets (used by Wayland text-input
/// protocols) and UTF-16 char indices (used by .NET <see cref="string"/>).
/// </summary>
/// <remarks>
/// The Wayland text-input protocols address text by byte offset into the UTF-8
/// encoding of the string. Avalonia / .NET use UTF-16 internally. Conversion
/// must respect multi-byte sequences and surrogate pairs (a single non-BMP
/// codepoint is 4 bytes in UTF-8 and 2 chars in UTF-16) and must be robust to
/// negative offsets, out-of-range indices and partially-truncated sequences.
/// </remarks>
internal static class WaylandTextUtils
{
    /// <summary>
    /// Convert a wayland-side byte offset (relative to <paramref name="baseChar"/>)
    /// into a UTF-16 char index in <paramref name="text"/>. A negative
    /// <paramref name="byteLength"/> means "go backwards from baseChar".
    /// Out-of-range offsets are clamped to <c>[0, text.Length]</c>.
    /// </summary>
    public static int CharIndexFromUtf8Offset(string text, int byteLength, int baseChar)
    {
        if (byteLength == 0)
            return Math.Clamp(baseChar, 0, text.Length);

        baseChar = Math.Clamp(baseChar, 0, text.Length);

        if (byteLength < 0)
        {
            var prefix = text.AsSpan(0, baseChar);
            var prefixBytes = System.Text.Encoding.UTF8.GetByteCount(prefix);
            var targetBytes = Math.Max(prefixBytes + byteLength, 0);
            return CountCharsForUtf8Bytes(prefix, targetBytes);
        }
        else
        {
            var suffix = text.AsSpan(baseChar);
            var suffixBytes = System.Text.Encoding.UTF8.GetByteCount(suffix);
            var targetBytes = Math.Min(byteLength, suffixBytes);
            return baseChar + CountCharsForUtf8Bytes(suffix, targetBytes);
        }
    }

    /// <summary>
    /// Convert a UTF-16 char index <paramref name="charIndex"/> in
    /// <paramref name="text"/> into a wayland-side byte offset, relative to
    /// <paramref name="baseChar"/>. The returned value is the byte distance
    /// between <paramref name="baseChar"/> and <paramref name="charIndex"/>;
    /// it is negative when <paramref name="charIndex"/> &lt; <paramref name="baseChar"/>.
    /// </summary>
    public static int Utf8OffsetFromCharIndex(string text, int charIndex, int baseChar)
    {
        if (charIndex == baseChar)
            return 0;

        baseChar = Math.Clamp(baseChar, 0, text.Length);
        charIndex = Math.Clamp(charIndex, 0, text.Length);

        if (charIndex >= baseChar)
            return System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(baseChar, charIndex - baseChar));

        return -System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(charIndex, baseChar - charIndex));
    }

    /// <summary>
    /// Convert an absolute char index to its absolute UTF-8 byte index in
    /// <paramref name="text"/>. Equivalent to
    /// <c>Utf8OffsetFromCharIndex(text, charIndex, 0)</c>.
    /// </summary>
    public static int Utf8ByteIndexFromCharIndex(string text, int charIndex)
    {
        charIndex = Math.Clamp(charIndex, 0, text.Length);
        return System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(0, charIndex));
    }

    /// <summary>
    /// Walk the UTF-16 <paramref name="span"/> until either it is exhausted or
    /// the cumulative UTF-8 byte count reaches <paramref name="targetBytes"/>.
    /// Stops at a UTF-16 char boundary that lies on a UTF-8 codepoint boundary,
    /// i.e. it never returns a position halfway through a surrogate pair.
    /// </summary>
    private static int CountCharsForUtf8Bytes(ReadOnlySpan<char> span, int targetBytes)
    {
        if (targetBytes <= 0)
            return 0;

        var bytes = 0;
        var i = 0;
        while (i < span.Length)
        {
            var c = span[i];
            int cpBytes;
            int cpChars;
            if (char.IsHighSurrogate(c) && i + 1 < span.Length && char.IsLowSurrogate(span[i + 1]))
            {
                cpBytes = 4;
                cpChars = 2;
            }
            else if (c < 0x80)
            {
                cpBytes = 1;
                cpChars = 1;
            }
            else if (c < 0x800)
            {
                cpBytes = 2;
                cpChars = 1;
            }
            else
            {
                cpBytes = 3;
                cpChars = 1;
            }

            if (bytes + cpBytes > targetBytes)
                break;

            bytes += cpBytes;
            i += cpChars;
        }

        return i;
    }
}
