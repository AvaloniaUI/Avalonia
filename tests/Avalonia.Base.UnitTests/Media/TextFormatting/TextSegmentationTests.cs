using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class TextSegmentationTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("ab", 1)]
    [InlineData("a\U0001F600", 1)]
    [InlineData("\u0915\u093F\u0903", 2)]
    [InlineData("e\u0301", 1)]
    [InlineData("a\U0001F44D\U0001F3FD", 3)]
    [InlineData("a1\uFE0F\u20E3", 3)]
    [InlineData("a\U0001F1EF\U0001F1F5\U0001F1FA", 5)]
    [InlineData("a\u200D", 1)]
    public void BackspaceStart_Should_Remove_Last_Codepoint(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }

    [Theory]
    [InlineData("a\r\n", 1)]
    [InlineData("a\r", 1)]
    public void BackspaceStart_Should_Remove_CRLF_As_A_Unit(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }

    [Theory]
    [InlineData("a\U0001F5C2\uFE0F", 1)]
    [InlineData("\u845B\U000E0100", 0)]
    [InlineData("a\n\uFE0F", 2)]
    [InlineData("a\r\n\uFE0F", 3)]
    [InlineData("a\u0301\uFE0F", 2)]
    public void BackspaceStart_Should_Remove_Variation_Selector_With_Its_Base(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }

    [Theory]
    [InlineData("a\U0001F1EF\U0001F1F5", 1)]
    [InlineData("\U0001F1EF\U0001F1F5\U0001F1FA\U0001F1F8", 4)]
    public void BackspaceStart_Should_Remove_Flag_As_A_Unit(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }

    [Theory]
    [InlineData("a\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F", 3)]
    [InlineData("a\n\U000E007F", 2)]
    public void BackspaceStart_Should_Remove_Tags_Of_A_Tag_Sequence_Together(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }

    [Theory]
    [InlineData("a\U0001F468\u200D\U0001F4BB", 3)]
    [InlineData("a\U0001F441\uFE0F\u200D\U0001F5E8\uFE0F", 4)]
    [InlineData("a\U0001F469\U0001F3FD\u200D\U0001F4BB", 5)]
    public void BackspaceStart_Should_Remove_Emoji_Joiner_With_The_Emoji_After_It(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }

    [Theory]
    [InlineData("\u0915\u094D\u200D\u0937", 3)]
    [InlineData("a\u200D\U0001F4BB", 2)]
    [InlineData("\U0001F468\u200Db", 3)]
    public void BackspaceStart_Should_Keep_Joiner_Outside_Of_Emoji_Sequences(string text, int expected)
    {
        Assert.Equal(expected, TextSegmentation.BackspaceStart(text.Length, text));
    }
}
