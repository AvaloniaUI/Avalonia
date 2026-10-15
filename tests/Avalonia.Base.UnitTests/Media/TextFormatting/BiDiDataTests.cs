using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class BiDiDataTests
{
    [Theory]
    [InlineData("\u202Eabc\u202C", 1)] // RLO ... PDF
    [InlineData("\u202Babc\u202C", 2)] // RLE ... PDF
    [InlineData("\u2067abc\u2069", 2)] // RLI ... PDI
    public void Explicit_Formatting_Should_Be_Resolved_After_Reset(string text, sbyte expectedLevel)
    {
        var bidiData = new BidiData();
        var bidi = new BidiAlgorithm();

        bidiData.Append("abc");
        bidi.Process(bidiData);

        bidiData.Reset();
        bidi.Reset();

        bidiData.Append(text);
        bidi.Process(bidiData);

        Assert.Equal(expectedLevel, bidi.ResolvedLevels[1]);
    }
}
