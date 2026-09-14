using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class WaylandTextUtilsTests
{
    // Cyrillic 'й' = U+0439 → 2 UTF-8 bytes, 1 char.
    // CJK '日' = U+65E5 → 3 UTF-8 bytes, 1 char.
    // Emoji '🌍' = U+1F30D → 4 UTF-8 bytes, 2 chars (surrogate pair).
    private const string Mixed = "aй日🌍b";   // bytes: 1+2+3+4+1=11; chars: 1+1+1+2+1=6.

    [Theory]
    [InlineData("hello", 0, 0, 0)]
    [InlineData("hello", 5, 0, 5)]                      // forward, ASCII
    [InlineData("hello", -3, 5, 2)]                     // backward, ASCII
    [InlineData("hello", 100, 0, 5)]                    // clamp forward
    [InlineData("hello", -100, 5, 0)]                   // clamp backward
    [InlineData("aй日🌍b", 0, 3, 3)]                     // zero distance from middle
    [InlineData("aй日🌍b", 1, 0, 1)]                     // 'a' = 1 byte
    [InlineData("aй日🌍b", 3, 0, 2)]                     // 'aй' = 3 bytes → 2 chars
    [InlineData("aй日🌍b", 6, 0, 3)]                     // 'aй日' = 6 bytes → 3 chars
    [InlineData("aй日🌍b", 10, 0, 5)]                    // 'aй日🌍' = 10 bytes → 5 chars (surrogate pair counts as 2)
    [InlineData("aй日🌍b", 11, 0, 6)]                    // full
    [InlineData("aй日🌍b", -1, 6, 5)]                    // 'b' = 1 byte
    [InlineData("aй日🌍b", -5, 6, 3)]                    // 'b' + '🌍' = 5 bytes → drops 3 chars
    [InlineData("aй日🌍b", -11, 6, 0)]                   // back to start
    [InlineData("aй日🌍b", 1, 1, 1)]                     // partway: from char index 1, +1 byte cannot reach a clean boundary in "й" (2 bytes), so stays at 1
    public void CharIndexFromUtf8Offset_Cases(string text, int byteLength, int baseChar, int expectedChar)
    {
        Assert.Equal(expectedChar, WaylandTextUtils.CharIndexFromUtf8Offset(text, byteLength, baseChar));
    }

    [Fact]
    public void CharIndexFromUtf8Offset_DoesNotSplitMultiByte()
    {
        // From start, +1 byte into "й" (2 bytes) — only the 'a' (1 byte) fits cleanly,
        // so we should land at char 1, not partway through 'й'.
        Assert.Equal(1, WaylandTextUtils.CharIndexFromUtf8Offset(Mixed, 2, 0));
        // +1 byte from char 1 should consume nothing since "й" alone is 2 bytes.
        Assert.Equal(1, WaylandTextUtils.CharIndexFromUtf8Offset(Mixed, 1, 1));
    }

    [Fact]
    public void CharIndexFromUtf8Offset_DoesNotSplitSurrogatePair()
    {
        // From start, +9 bytes = "aй日" (6) + 3 bytes into '🌍' (4) — should NOT
        // return half a surrogate pair. Should clamp at "aй日" = 3 chars.
        Assert.Equal(3, WaylandTextUtils.CharIndexFromUtf8Offset(Mixed, 9, 0));
    }

    [Theory]
    [InlineData("hello", 0, 0, 0)]
    [InlineData("hello", 5, 0, 5)]
    [InlineData("hello", 2, 5, -3)]
    [InlineData("aй日🌍b", 0, 0, 0)]
    [InlineData("aй日🌍b", 1, 0, 1)]
    [InlineData("aй日🌍b", 2, 0, 3)]
    [InlineData("aй日🌍b", 3, 0, 6)]
    [InlineData("aй日🌍b", 5, 0, 10)]                    // through the surrogate pair
    [InlineData("aй日🌍b", 6, 0, 11)]
    [InlineData("aй日🌍b", 5, 6, -1)]                    // backward
    [InlineData("aй日🌍b", 3, 6, -5)]                    // backward through surrogate pair
    public void Utf8OffsetFromCharIndex_Cases(string text, int charIndex, int baseChar, int expectedBytes)
    {
        Assert.Equal(expectedBytes, WaylandTextUtils.Utf8OffsetFromCharIndex(text, charIndex, baseChar));
    }

    [Fact]
    public void Utf8OffsetFromCharIndex_ClampsOutOfRange()
    {
        Assert.Equal(11, WaylandTextUtils.Utf8OffsetFromCharIndex(Mixed, 100, 0));
        Assert.Equal(-11, WaylandTextUtils.Utf8OffsetFromCharIndex(Mixed, -10, 6));
    }

    [Fact]
    public void RoundTrip_AllCharBoundaries()
    {
        // For every char index, char→byte→char should be the identity (modulo
        // surrogate-pair midpoints, which we deliberately avoid by stepping through
        // char positions, not byte positions).
        for (var i = 0; i <= Mixed.Length; i++)
        {
            // skip the low-surrogate position (mid-pair)
            if (i > 0 && char.IsLowSurrogate(Mixed[i - 1]) == false &&
                i < Mixed.Length && char.IsHighSurrogate(Mixed[i]) && char.IsLowSurrogate(Mixed[i + 1]))
            {
                // i points at a high surrogate — that's a valid boundary.
            }

            if (i > 0 && i < Mixed.Length && char.IsLowSurrogate(Mixed[i]))
                continue;

            var bytes = WaylandTextUtils.Utf8ByteIndexFromCharIndex(Mixed, i);
            var roundTrip = WaylandTextUtils.CharIndexFromUtf8Offset(Mixed, bytes, 0);
            Assert.Equal(i, roundTrip);
        }
    }
}
