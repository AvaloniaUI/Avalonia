using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

/// <summary>
/// Lightweight round-trip checks for the generated <see cref="PropertyValueAliasHelper"/>.
/// Catches regressions in the alias-helper writer (e.g. wrong typeName, missing
/// entries, casing mismatches) without re-asserting the UCD aliases themselves.
/// </summary>
public class PropertyValueAliasHelperTests
{
    [Theory]
    [InlineData("L", BidiClass.LeftToRight)]
    [InlineData("R", BidiClass.RightToLeft)]
    [InlineData("AL", BidiClass.ArabicLetter)]
    [InlineData("EN", BidiClass.EuropeanNumber)]
    public void GetBidiClass_KnownTags(string tag, BidiClass expected)
    {
        Assert.Equal(expected, PropertyValueAliasHelper.GetBidiClass(tag));
    }

    [Fact]
    public void GetBidiClass_UnknownTag_FallsBackToLeftToRight()
    {
        // The generator emits LeftToRight as the fallback for unknown tags.
        Assert.Equal(BidiClass.LeftToRight, PropertyValueAliasHelper.GetBidiClass("not-a-real-tag"));
    }

    [Theory]
    [InlineData("Latn", Script.Latin)]
    [InlineData("Cyrl", Script.Cyrillic)]
    [InlineData("Hani", Script.Han)]
    [InlineData("Hebr", Script.Hebrew)]
    [InlineData("Arab", Script.Arabic)]
    [InlineData("Zyyy", Script.Common)]
    public void GetScript_KnownTags(string tag, Script expected)
    {
        Assert.Equal(expected, PropertyValueAliasHelper.GetScript(tag));
    }

    [Fact]
    public void GetTag_RoundTripsScriptThroughGetScript()
    {
        // Tag -> enum -> tag should round-trip for every script the helper knows.
        // Pick a handful so we catch obvious typoes in the writer without needing
        // a full mirror of UCD here.
        foreach (var script in new[] { Script.Latin, Script.Cyrillic, Script.Han, Script.Hebrew, Script.Arabic })
        {
            var tag = PropertyValueAliasHelper.GetTag(script);
            Assert.Equal(script, PropertyValueAliasHelper.GetScript(tag));
        }
    }

    [Theory]
    [InlineData("Lu", GeneralCategory.UppercaseLetter)]
    [InlineData("Ll", GeneralCategory.LowercaseLetter)]
    [InlineData("Nd", GeneralCategory.DecimalNumber)]
    [InlineData("Zs", GeneralCategory.SpaceSeparator)]
    [InlineData("Cc", GeneralCategory.Control)]
    public void GetGeneralCategory_KnownTags(string tag, GeneralCategory expected)
    {
        Assert.Equal(expected, PropertyValueAliasHelper.GetGeneralCategory(tag));
    }

    [Theory]
    [InlineData("AL", LineBreakClass.Alphabetic)]
    [InlineData("LF", LineBreakClass.LineFeed)]
    [InlineData("CR", LineBreakClass.CarriageReturn)]
    [InlineData("XX", LineBreakClass.Unknown)]
    public void GetLineBreakClass_KnownTags(string tag, LineBreakClass expected)
    {
        Assert.Equal(expected, PropertyValueAliasHelper.GetLineBreakClass(tag));
    }

    [Theory]
    [InlineData("LE", WordBreakClass.ALetter)]
    [InlineData("CR", WordBreakClass.CarriageReturn)]
    [InlineData("LF", WordBreakClass.LineFeed)]
    [InlineData("XX", WordBreakClass.Other)]
    public void GetWordBreakClass_KnownTags(string tag, WordBreakClass expected)
    {
        Assert.Equal(expected, PropertyValueAliasHelper.GetWordBreakClass(tag));
    }

    [Theory]
    [InlineData("o", BidiPairedBracketType.Open)]
    [InlineData("c", BidiPairedBracketType.Close)]
    [InlineData("n", BidiPairedBracketType.None)]
    public void GetBidiPairedBracketType_KnownTags(string tag, BidiPairedBracketType expected)
    {
        Assert.Equal(expected, PropertyValueAliasHelper.GetBidiPairedBracketType(tag));
    }
}
