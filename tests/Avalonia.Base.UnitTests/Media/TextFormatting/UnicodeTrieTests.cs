using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

/// <summary>
/// Direct coverage for <see cref="UnicodeTrie"/> and <see cref="UnicodeTrieBuilder"/>.
/// The production tries (UnicodeData, BiDi, GraphemeBreak, EastAsianWidth) are
/// used to exercise the four branches of <see cref="UnicodeTrie.Get"/>; a small
/// synthetic trie covers the error-value path (which is otherwise unreachable
/// because the committed tries are generated with <c>errorValue == 0</c>) and the
/// builder round-trip.
/// </summary>
public class UnicodeTrieTests
{
    [Theory]
    [InlineData(0x0020u)] // ASCII space
    [InlineData(0x0061u)] // 'a'
    [InlineData(0x4E2Du)] // '中' (BMP CJK)
    [InlineData(0xFFFFu)] // last BMP non-surrogate
    public void Get_BmpNonSurrogate_ReturnsValueMatchingUnicodeDataWrapper(uint codepoint)
    {
        // Walking the trie directly and re-applying the published shift/mask must
        // produce the same answer as the public UnicodeData wrapper. This catches
        // packing-layout drift between generator (writes packed bits) and
        // UnicodeData.Get* (reads packed bits).
        var packed = UnicodeDataTrie.Trie.Get(codepoint);

        var categoryFromTrie = (GeneralCategory)(packed & UnicodeData.CATEGORY_MASK);
        var scriptFromTrie = (Script)((packed >> UnicodeData.SCRIPT_SHIFT) & UnicodeData.SCRIPT_MASK);

        Assert.Equal(UnicodeData.GetGeneralCategory(codepoint), categoryFromTrie);
        Assert.Equal(UnicodeData.GetScript(codepoint), scriptFromTrie);
    }

    [Theory]
    [InlineData(0xD800u)] // first high surrogate
    [InlineData(0xDB00u)] // mid high surrogate
    [InlineData(0xDBFFu)] // last high surrogate
    [InlineData(0xDC00u)] // first low surrogate
    [InlineData(0xDFFFu)] // last low surrogate
    public void Get_SurrogateRange_ResolvesViaLscpIndex(uint codepoint)
    {
        // Surrogates have a dedicated index region (LSCP_INDEX_2_OFFSET) in the
        // trie. The general category for every codepoint in this range is
        // Surrogate; this asserts the LSCP branch returns the correct row.
        Assert.Equal(GeneralCategory.Surrogate, UnicodeData.GetGeneralCategory(codepoint));
    }

    [Theory]
    [InlineData(0x10000u)] // first supplementary
    [InlineData(0x1F600u)] // 😀
    [InlineData(0x2F800u)] // CJK compatibility supplement
    public void Get_Supplementary_BelowHighStart_ResolvesViaTwoLevelLookup(uint codepoint)
    {
        // Just walking the trie and reapplying the published mask must match the
        // wrapper — same guarantee as the BMP test, but exercises the two-level
        // supplementary lookup branch.
        var packed = BiDiTrie.Trie.Get(codepoint);
        var bidiFromTrie = (BidiClass)((packed >> UnicodeData.BIDICLASS_SHIFT) & UnicodeData.BIDICLASS_MASK);

        Assert.Equal(UnicodeData.GetBiDiClass(codepoint), bidiFromTrie);
    }

    [Fact]
    public void Get_AtAndAboveHighStart_AllCodepointsShareFallback()
    {
        // Every codepoint >= HighStart short-circuits to the trie's last data
        // block. The committed tries set HighStart at 0x100000, so all of Plane
        // 16 collapses to one fallback value — this is a compression artifact
        // of the trie format. The test verifies the SHAPE of that contract (one
        // value for the whole high range) rather than asserting any specific
        // per-codepoint property: callers querying Plane 16 should not rely on
        // PUA / Unassigned distinctions surviving the trie.
        var v100000 = UnicodeDataTrie.Trie.Get(0x100000u);
        Assert.Equal(v100000, UnicodeDataTrie.Trie.Get(0x100001u));
        Assert.Equal(v100000, UnicodeDataTrie.Trie.Get(0x10FFFDu));
        Assert.Equal(v100000, UnicodeDataTrie.Trie.Get(0x10FFFFu));

        var bidi100000 = BiDiTrie.Trie.Get(0x100000u);
        Assert.Equal(bidi100000, BiDiTrie.Trie.Get(0x10FFFFu));
    }

    [Fact]
    public void Get_BeyondMaxCodepoint_IsHandledGracefullyByProductionTries()
    {
        // The committed tries are generated with errorValue == 0, so codepoints
        // past 0x10FFFF return 0 (Other category, LeftToRight bidi, etc.). This
        // documents that contract; the synthetic-trie test below covers the
        // case where errorValue is non-zero.
        const uint beyondRange = 0x110000u;

        Assert.Equal(0u, UnicodeDataTrie.Trie.Get(beyondRange));
        Assert.Equal(0u, BiDiTrie.Trie.Get(beyondRange));
        Assert.Equal(0u, GraphemeBreakTrie.Trie.Get(beyondRange));
        Assert.Equal(0u, EastAsianWidthTrie.Trie.Get(beyondRange));
    }

    [Fact]
    public void Builder_RoundTripsSetValues()
    {
        var builder = new UnicodeTrieBuilder(initialValue: 7u);
        builder.Set(0x0061, 0xAA);
        builder.Set(0x4E2D, 0xBB);
        builder.Set(0x1F600, 0xCC);

        var trie = builder.Freeze();

        Assert.Equal(0xAAu, trie.Get(0x0061));
        Assert.Equal(0xBBu, trie.Get(0x4E2D));
        Assert.Equal(0xCCu, trie.Get(0x1F600));
    }

    [Fact]
    public void Builder_SetRange_AppliesValueToEveryCodepointInRange()
    {
        var builder = new UnicodeTrieBuilder();
        builder.SetRange(0x2000, 0x2010, 0x42);

        var trie = builder.Freeze();

        for (uint cp = 0x2000; cp <= 0x2010; cp++)
        {
            Assert.Equal(0x42u, trie.Get(cp));
        }

        // Just outside the range stays at the initial value (0 by default).
        Assert.Equal(0u, trie.Get(0x1FFF));
        Assert.Equal(0u, trie.Get(0x2011));
    }

    [Fact]
    public void Builder_UnassignedCodepoints_GetInitialValue()
    {
        var builder = new UnicodeTrieBuilder(initialValue: 0xDEAD);
        builder.Set(0x0061, 0xBEEF);

        var trie = builder.Freeze();

        Assert.Equal(0xBEEFu, trie.Get(0x0061));
        Assert.Equal(0xDEADu, trie.Get(0x0062));
        Assert.Equal(0xDEADu, trie.Get(0x4E2D));
        Assert.Equal(0xDEADu, trie.Get(0x1F600));
    }

    [Fact]
    public void Get_OutOfRange_ReturnsConfiguredErrorValue()
    {
        // Build with a non-zero errorValue so the "> 0x10FFFF" branch produces a
        // distinguishable result. This is the only feasible test of that branch —
        // the committed tries all use errorValue == 0 which collides with the
        // happy-path zero value.
        var builder = new UnicodeTrieBuilder(initialValue: 0u, errorValue: 0xFFFFu);
        builder.Set(0x0061, 0x11);

        var trie = builder.Freeze();

        Assert.Equal(0xFFFFu, trie.Get(0x110000u));
        Assert.Equal(0xFFFFu, trie.Get(0xFFFFFFFFu));
    }

    [Fact]
    public void Get_AtAndAboveHighStart_OnSyntheticTrie_UsesHighFallback()
    {
        // SetRange across a huge supplementary span forces the builder to allocate
        // a high block. Codepoints at and above the resulting HighStart should
        // return the value that covers the high range.
        var builder = new UnicodeTrieBuilder(initialValue: 0u);
        builder.SetRange(0x80000, 0x10FFFF, 0x55);

        var trie = builder.Freeze();

        Assert.Equal(0x55u, trie.Get(0x100000u));
        Assert.Equal(0x55u, trie.Get(0x10FFFFu));

        // Below the high range — still the initial value.
        Assert.Equal(0u, trie.Get(0x1000u));
    }
}
