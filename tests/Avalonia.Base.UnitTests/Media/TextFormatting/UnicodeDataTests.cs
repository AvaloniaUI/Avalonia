using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

/// <summary>
/// Spot-checks for <see cref="UnicodeData"/>. The trie generator already round-trips
/// every assigned codepoint against its source dictionary, so these tests focus on
/// pinning the public surface against drift: shift/mask layout, default values for
/// unassigned codepoints, and a few well-known codepoints across the four tries.
/// </summary>
public class UnicodeDataTests
{
    [Theory]
    [InlineData(0x0061u, GeneralCategory.LowercaseLetter)]   // 'a'
    [InlineData(0x0041u, GeneralCategory.UppercaseLetter)]   // 'A'
    [InlineData(0x0030u, GeneralCategory.DecimalNumber)]     // '0'
    [InlineData(0x0020u, GeneralCategory.SpaceSeparator)]    // ' '
    [InlineData(0x000Au, GeneralCategory.Control)]           // '\n'
    [InlineData(0x0009u, GeneralCategory.Control)]           // '\t'
    [InlineData(0x002Eu, GeneralCategory.OtherPunctuation)]  // '.'
    [InlineData(0x0028u, GeneralCategory.OpenPunctuation)]   // '('
    [InlineData(0x0029u, GeneralCategory.ClosePunctuation)]  // ')'
    [InlineData(0x0024u, GeneralCategory.CurrencySymbol)]    // '$'
    [InlineData(0x002Bu, GeneralCategory.MathSymbol)]        // '+'
    [InlineData(0x200Bu, GeneralCategory.Format)]            // ZWSP
    [InlineData(0xE000u, GeneralCategory.PrivateUse)]        // BMP PUA start
    [InlineData(0xD800u, GeneralCategory.Surrogate)]         // high surrogate start (LSCP path)
    [InlineData(0xDFFFu, GeneralCategory.Surrogate)]         // low surrogate end (LSCP path)
    // Note: codepoints >= HighStart (currently 0x100000) all collapse to a single
    // fallback value because the trie compresses Plane 16 to save space. The
    // resulting GeneralCategory is not the per-codepoint UCD value. See
    // UnicodeTrieTests.Get_AtAndAboveHighStart_AllCodepointsShareFallback.
    public void GetGeneralCategory_KnownCodepoints(uint codepoint, GeneralCategory expected)
    {
        Assert.Equal(expected, UnicodeData.GetGeneralCategory(codepoint));
    }

    [Theory]
    [InlineData(0x0061u, Script.Latin)]        // 'a'
    [InlineData(0x0041u, Script.Latin)]        // 'A'
    [InlineData(0x044Fu, Script.Cyrillic)]     // 'я'
    [InlineData(0x4E2Du, Script.Han)]          // '中'
    [InlineData(0x05D0u, Script.Hebrew)]       // 'א'
    [InlineData(0x0627u, Script.Arabic)]       // 'ا'
    [InlineData(0x0020u, Script.Common)]       // ' '
    [InlineData(0x0030u, Script.Common)]       // '0'
    [InlineData(0x1F600u, Script.Common)]      // 😀 (supplementary, common)
    [InlineData(0x0300u, Script.Inherited)]    // combining grave (inherited)
    public void GetScript_KnownCodepoints(uint codepoint, Script expected)
    {
        Assert.Equal(expected, UnicodeData.GetScript(codepoint));
    }

    [Theory]
    [InlineData(0x0061u, BidiClass.LeftToRight)]        // 'a'
    [InlineData(0x0041u, BidiClass.LeftToRight)]        // 'A'
    [InlineData(0x0039u, BidiClass.EuropeanNumber)]     // '9'
    [InlineData(0x0024u, BidiClass.EuropeanTerminator)] // '$'
    [InlineData(0x002Cu, BidiClass.CommonSeparator)]    // ','
    [InlineData(0x0020u, BidiClass.WhiteSpace)]         // ' '
    [InlineData(0x0009u, BidiClass.SegmentSeparator)]   // '\t'
    [InlineData(0x000Au, BidiClass.ParagraphSeparator)] // '\n'
    [InlineData(0x05D0u, BidiClass.RightToLeft)]        // 'א'
    [InlineData(0x0627u, BidiClass.ArabicLetter)]       // 'ا'
    public void GetBiDiClass_KnownCodepoints(uint codepoint, BidiClass expected)
    {
        Assert.Equal(expected, UnicodeData.GetBiDiClass(codepoint));
    }

    [Theory]
    [InlineData(0x0028u, BidiPairedBracketType.Open, 0x0029u)]  // '(' → ')'
    [InlineData(0x0029u, BidiPairedBracketType.Close, 0x0028u)] // ')' → '('
    [InlineData(0x005Bu, BidiPairedBracketType.Open, 0x005Du)]  // '[' → ']'
    [InlineData(0x005Du, BidiPairedBracketType.Close, 0x005Bu)] // ']' → '['
    [InlineData(0x007Bu, BidiPairedBracketType.Open, 0x007Du)]  // '{' → '}'
    public void GetBiDiPairedBracket_RoundTripsKnownPairs(uint codepoint, BidiPairedBracketType expectedType, uint expectedPair)
    {
        Assert.Equal(expectedType, UnicodeData.GetBiDiPairedBracketType(codepoint));
        Assert.Equal(expectedPair, UnicodeData.GetBiDiPairedBracket(codepoint).Value);
    }

    [Fact]
    public void GetBiDiPairedBracketType_NonBracket_IsNone()
    {
        Assert.Equal(BidiPairedBracketType.None, UnicodeData.GetBiDiPairedBracketType(0x0061u));
        Assert.Equal(BidiPairedBracketType.None, UnicodeData.GetBiDiPairedBracketType(0x0020u));
    }

    [Theory]
    [InlineData(0x0061u, LineBreakClass.Alphabetic)]      // 'a'
    [InlineData(0x000Au, LineBreakClass.LineFeed)]        // '\n'
    [InlineData(0x000Du, LineBreakClass.CarriageReturn)]  // '\r'
    [InlineData(0x0020u, LineBreakClass.Space)]           // ' '
    [InlineData(0x0009u, LineBreakClass.BreakAfter)]      // '\t'
    [InlineData(0x002Du, LineBreakClass.Hyphen)]          // '-'
    [InlineData(0x0028u, LineBreakClass.OpenPunctuation)] // '('
    [InlineData(0x0029u, LineBreakClass.CloseParenthesis)]// ')'
    [InlineData(0x0030u, LineBreakClass.Numeric)]         // '0'
    [InlineData(0x4E2Du, LineBreakClass.Ideographic)]     // '中'
    [InlineData(0x2028u, LineBreakClass.MandatoryBreak)]  // LINE SEPARATOR
    [InlineData(0x2029u, LineBreakClass.MandatoryBreak)]  // PARAGRAPH SEPARATOR
    public void GetLineBreakClass_KnownCodepoints(uint codepoint, LineBreakClass expected)
    {
        Assert.Equal(expected, UnicodeData.GetLineBreakClass(codepoint));
    }

    [Theory]
    [InlineData(0x0061u, WordBreakClass.ALetter)]         // 'a'
    [InlineData(0x000Du, WordBreakClass.CarriageReturn)]  // '\r'
    [InlineData(0x000Au, WordBreakClass.LineFeed)]        // '\n'
    [InlineData(0x0020u, WordBreakClass.WSegSpace)]       // ' '
    [InlineData(0x0030u, WordBreakClass.Numeric)]         // '0'
    [InlineData(0x200Du, WordBreakClass.ZWJ)]             // ZWJ
    [InlineData(0x05D0u, WordBreakClass.HebrewLetter)]    // 'א'
    [InlineData(0x4E2Du, WordBreakClass.Other)]           // '中' (CJK is WB=Other)
    public void GetWordBreakClass_KnownCodepoints(uint codepoint, WordBreakClass expected)
    {
        Assert.Equal(expected, UnicodeData.GetWordBreakClass(codepoint));
    }

    [Theory]
    [InlineData(0x000Du, GraphemeBreakClass.CR)]                    // '\r'
    [InlineData(0x000Au, GraphemeBreakClass.LF)]                    // '\n'
    [InlineData(0x200Du, GraphemeBreakClass.ZWJ)]                   // ZWJ
    [InlineData(0x1F600u, GraphemeBreakClass.ExtendedPictographic)] // 😀 (overridden by emoji-data.txt)
    [InlineData(0x1100u, GraphemeBreakClass.L)]                     // HANGUL CHOSEONG KIYEOK
    [InlineData(0x1161u, GraphemeBreakClass.V)]                     // HANGUL JUNGSEONG A
    [InlineData(0x11A8u, GraphemeBreakClass.T)]                     // HANGUL JONGSEONG KIYEOK
    [InlineData(0x0061u, GraphemeBreakClass.Other)]                 // 'a'
    [InlineData(0x0030u, GraphemeBreakClass.Other)]                 // '0'
    public void GetGraphemeClusterBreak_KnownCodepoints(uint codepoint, GraphemeBreakClass expected)
    {
        Assert.Equal(expected, UnicodeData.GetGraphemeClusterBreak(codepoint));
    }

    [Theory]
    [InlineData(0x0061u, EastAsianWidthClass.Narrow)]    // 'a'
    [InlineData(0x0020u, EastAsianWidthClass.Narrow)]    // ' '
    [InlineData(0x4E2Du, EastAsianWidthClass.Wide)]      // '中'
    [InlineData(0xFF21u, EastAsianWidthClass.Fullwidth)] // 'Ａ' FULLWIDTH LATIN CAPITAL A
    [InlineData(0xFF71u, EastAsianWidthClass.Halfwidth)] // 'ｱ' HALFWIDTH KATAKANA A
    [InlineData(0x03B1u, EastAsianWidthClass.Ambiguous)] // 'α'
    [InlineData(0x200Bu, EastAsianWidthClass.Neutral)]   // ZWSP
    public void GetEastAsianWidthClass_KnownCodepoints(uint codepoint, EastAsianWidthClass expected)
    {
        Assert.Equal(expected, UnicodeData.GetEastAsianWidthClass(codepoint));
    }

    /// <summary>
    /// Regression test for the BiDi / GraphemeBreak / UnicodeData trie builders'
    /// reliance on the seeded default class sitting at int position 0 (caught at
    /// generation time by the ABI validator, but only this asserts the runtime
    /// behavior of an unassigned codepoint).
    /// </summary>
    [Fact]
    public void UnassignedCodepoint_FallsBackToSeededDefaults()
    {
        // U+0378 is an unassigned BMP code point (and has been for decades — stable choice).
        const uint unassigned = 0x0378u;

        // Default Bidi class for unassigned codepoints is LeftToRight (seeded at position 0).
        Assert.Equal(BidiClass.LeftToRight, UnicodeData.GetBiDiClass(unassigned));

        // Default grapheme break class is Other (seeded at position 0).
        Assert.Equal(GraphemeBreakClass.Other, UnicodeData.GetGraphemeClusterBreak(unassigned));

        // Default line break class is Unknown — set explicitly via initialValue in the
        // UnicodeData trie builder, not via seed position 0.
        Assert.Equal(LineBreakClass.Unknown, UnicodeData.GetLineBreakClass(unassigned));

        // Default word break class is Other — also set explicitly in the generator's
        // post-pass that maps unset WordBreakClass to Other.
        Assert.Equal(WordBreakClass.Other, UnicodeData.GetWordBreakClass(unassigned));
    }
}
