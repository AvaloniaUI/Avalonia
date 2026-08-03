using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

/// <summary>
/// Direct coverage for <see cref="Codepoint"/> — surrogate-pair decoding via
/// <see cref="Codepoint.ReadAt(ReadOnlySpan{char}, int, out int)"/>, the small
/// helper properties / methods, and the bitmask-based <c>IsWhiteSpace</c> path
/// that depends on every used <see cref="GeneralCategory"/> value fitting in 64
/// bits.
/// </summary>
public class CodepointTests
{
    [Theory]
    [InlineData("a", 0, (uint)'a', 1)]
    [InlineData("abc", 1, (uint)'b', 1)]
    [InlineData("abc", 2, (uint)'c', 1)]
    public void ReadAt_BmpScalar_ReturnsCharAndAdvancesByOne(string text, int index, uint expectedValue, int expectedCount)
    {
        var cp = Codepoint.ReadAt(text.AsSpan(), index, out var count);

        Assert.Equal(expectedValue, cp.Value);
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void ReadAt_HighSurrogate_AtStart_DecodesPair()
    {
        // U+1F600 GRINNING FACE — high surrogate at index 0.
        const string text = "😀";

        var cp = Codepoint.ReadAt(text.AsSpan(), 0, out var count);

        Assert.Equal(0x1F600u, cp.Value);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ReadAt_LowSurrogate_ScansBackToHighSurrogate()
    {
        // Reading at the low surrogate position should still return the full
        // supplementary codepoint by looking one index back.
        const string text = "😀";

        var cp = Codepoint.ReadAt(text.AsSpan(), 1, out var count);

        Assert.Equal(0x1F600u, cp.Value);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ReadAt_HighSurrogate_WithoutFollowingLow_ReturnsReplacement()
    {
        // Lone high surrogate at end of string.
        const string text = "a\uD83D";

        var cp = Codepoint.ReadAt(text.AsSpan(), 1, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReadAt_HighSurrogate_FollowedByNonLow_ReturnsReplacement()
    {
        // High surrogate followed by a regular BMP character (invalid pair).
        const string text = "\uD83Da";

        var cp = Codepoint.ReadAt(text.AsSpan(), 0, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReadAt_LoneLowSurrogate_AtStart_ReturnsReplacement()
    {
        // Lone low surrogate with nothing before it.
        const string text = "\uDE00b";

        var cp = Codepoint.ReadAt(text.AsSpan(), 0, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReadAt_LowSurrogate_NotPrecededByHigh_ReturnsReplacement()
    {
        // Low surrogate at index 1, but index 0 is a regular char (not a high surrogate).
        const string text = "a\uDE00";

        var cp = Codepoint.ReadAt(text.AsSpan(), 1, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReadAt_IndexPastLength_ReturnsReplacement()
    {
        const string text = "abc";

        var cp = Codepoint.ReadAt(text.AsSpan(), 5, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReadAt_IndexAtLength_ReturnsReplacement()
    {
        const string text = "abc";

        var cp = Codepoint.ReadAt(text.AsSpan(), 3, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReadAt_EmptySpan_ReturnsReplacement()
    {
        var cp = Codepoint.ReadAt(ReadOnlySpan<char>.Empty, 0, out var count);

        Assert.Equal(Codepoint.ReplacementCodepoint.Value, cp.Value);
        Assert.Equal(1, count);
    }

    [Fact]
    public void CodepointEnumerator_DecodesMixedBmpAndSupplementaryText()
    {
        // 'a' + 😀 (U+1F600) + 'b' + ✓ (U+2713) + 🚀 (U+1F680).
        const string text = "a😀b✓🚀";

        var expected = new uint[] { 'a', 0x1F600, 'b', 0x2713, 0x1F680 };

        var enumerator = new CodepointEnumerator(text.AsSpan());
        var actual = new List<uint>();
        while (enumerator.MoveNext(out var cp))
        {
            actual.Add(cp.Value);
        }

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// <see cref="Codepoint.IsWhiteSpace"/> uses a bitmask trick that assumes every
    /// <see cref="GeneralCategory"/> value used in the mask fits in 64 bits.
    /// If <c>Control</c>, <c>Format</c>, or <c>SpaceSeparator</c> ever moves past
    /// position 63 in the enum, the mask silently produces wrong results. This
    /// guards against that.
    /// </summary>
    [Fact]
    public void IsWhiteSpace_AllMaskedGeneralCategoriesFitInBitmask()
    {
        Assert.True((int)GeneralCategory.Control < 64);
        Assert.True((int)GeneralCategory.Format < 64);
        Assert.True((int)GeneralCategory.SpaceSeparator < 64);
    }

    [Theory]
    [InlineData(0x0020u, true)]   // SPACE
    [InlineData(0x0009u, true)]   // TAB (Control)
    [InlineData(0x000Au, true)]   // LF (Control)
    [InlineData(0x000Du, true)]   // CR (Control)
    [InlineData(0x00A0u, true)]   // NBSP (SpaceSeparator)
    [InlineData(0x200Bu, true)]   // ZWSP (Format)
    [InlineData(0x0061u, false)]  // 'a'
    [InlineData(0x0030u, false)]  // '0'
    [InlineData(0x002Eu, false)]  // '.'
    public void IsWhiteSpace_KnownCodepoints(uint value, bool expected)
    {
        Assert.Equal(expected, new Codepoint(value).IsWhiteSpace);
    }

    [Theory]
    [InlineData(0x000Au, true)] // LF
    [InlineData(0x000Bu, true)] // VT
    [InlineData(0x000Cu, true)] // FF
    [InlineData(0x000Du, true)] // CR
    [InlineData(0x0085u, true)] // NEL
    [InlineData(0x2028u, true)] // LINE SEPARATOR
    [InlineData(0x2029u, true)] // PARAGRAPH SEPARATOR
    [InlineData(0x0020u, false)]
    [InlineData(0x0061u, false)]
    [InlineData(0x0009u, false)] // TAB is not a "break" char in Avalonia's sense
    public void IsBreakChar_KnownCodepoints(uint value, bool expected)
    {
        Assert.Equal(expected, new Codepoint(value).IsBreakChar);
    }

    [Theory]
    [InlineData(0x0061u, false)] // 'a'
    [InlineData(0x4E2Du, true)]  // '中' Wide
    [InlineData(0xFF21u, true)]  // 'Ａ' Fullwidth
    [InlineData(0xFF71u, true)]  // 'ｱ' Halfwidth
    [InlineData(0x03B1u, false)] // 'α' Ambiguous (not east asian per IsEastAsian)
    [InlineData(0x0020u, false)] // ' ' Narrow
    public void IsEastAsian_KnownCodepoints(uint value, bool expected)
    {
        Assert.Equal(expected, new Codepoint(value).IsEastAsian);
    }

    [Theory]
    [InlineData(0x3008u, 0x2329u)] // 〈 → ⟨
    [InlineData(0x3009u, 0x232Au)] // 〉 → ⟩
    [InlineData(0x0061u, 0x0061u)] // 'a' → 'a' (unchanged)
    [InlineData(0x0028u, 0x0028u)] // '(' → '(' (unchanged)
    public void GetCanonicalType_MapsKnownCodepoints(uint input, uint expected)
    {
        // GetCanonicalType is internal; reachable here via InternalsVisibleTo.
        var actual = Codepoint.GetCanonicalType(new Codepoint(input));

        Assert.Equal(expected, actual.Value);
    }

    [Theory]
    [InlineData(0x0028u, true, 0x0029u)]  // '(' → ')'
    [InlineData(0x0029u, true, 0x0028u)]  // ')' → '('
    [InlineData(0x005Bu, true, 0x005Du)]  // '[' → ']'
    [InlineData(0x005Du, true, 0x005Bu)]  // ']' → '['
    [InlineData(0x0061u, false, 0u)]      // 'a' has no pair
    [InlineData(0x0020u, false, 0u)]      // ' ' has no pair
    public void TryGetPairedBracket_KnownCodepoints(uint codepoint, bool expectedSuccess, uint expectedPair)
    {
        var result = new Codepoint(codepoint).TryGetPairedBracket(out var pair);

        Assert.Equal(expectedSuccess, result);

        if (expectedSuccess)
        {
            Assert.Equal(expectedPair, pair.Value);
        }
    }

    [Fact]
    public void ImplicitConversions_RoundTripValue()
    {
        var cp = new Codepoint(0x1F600u);

        int asInt = cp;
        uint asUint = cp;

        Assert.Equal(0x1F600, asInt);
        Assert.Equal(0x1F600u, asUint);
    }

    [Fact]
    public void IsInRangeInclusive_BoundsAreInclusive()
    {
        Assert.True(Codepoint.IsInRangeInclusive(new Codepoint(0x10u), 0x10u, 0x20u));
        Assert.True(Codepoint.IsInRangeInclusive(new Codepoint(0x20u), 0x10u, 0x20u));
        Assert.True(Codepoint.IsInRangeInclusive(new Codepoint(0x15u), 0x10u, 0x20u));
        Assert.False(Codepoint.IsInRangeInclusive(new Codepoint(0x0Fu), 0x10u, 0x20u));
        Assert.False(Codepoint.IsInRangeInclusive(new Codepoint(0x21u), 0x10u, 0x20u));
    }
}
