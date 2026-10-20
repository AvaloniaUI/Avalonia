using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class BoundedLineBreakTests
{
    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("unbrokenwordwithseveralletters")]
    [InlineData("one  two   three   ")]
    [InlineData("     ")]
    [InlineData("Hello   \r\nWorld")]
    [InlineData("\r\n \n \r next")]
    [InlineData("one\t two\tthree")]
    [InlineData("'quoted text' (123,456.78) next")]
    [InlineData("(     text) \"   more\" next")]
    [InlineData("\u201c  quoted  text\u201d next")]
    [InlineData("日本語の文章と句読点。続き")]
    [InlineData("مرحبا بالعالم العربية")]
    [InlineData("বাংলা লেখা এবং শব্দ")]
    [InlineData("देवनागरी पाठ और शब्द")]
    [InlineData("emoji 😀 👩‍💻 🇨🇦🇯🇵 combining e\u0301")]
    [InlineData("word\u00a0word\u2060word\u200bword\u00adword")]
    [InlineData("a (12.34) 56,789% +99 /next")]
    [InlineData("a\u2028b\u0085c\u000bd")]
    public void Bounded_Search_Retains_The_Fitting_Prefix_Of_Complete_Enumeration(string text)
        => CheckEveryLimit(text);

    [Fact]
    public void Bounded_Search_Retains_Boundaries_Across_Unicode_Class_Pairs()
    {
        string[] units = ["\t", "\n", "\v", "\r", " ", "\"", "(", ")", ",", "-", "/", "0", "?", "a",
            "\u00a0", "\u00ad", "\u0301", "\u05d0", "\u0628", "\u1100", "\u1160", "\u11a8",
            "\u200b", "\u200d", "\u2014", "\u2024", "\u2060", "\u231a", "\u3001", "\u3041",
            "\u4e00", "\uac00", "\uac01", "\ufffc", "🇨", "👍", "🏽"];
        foreach (var prefix in new[] { "", "(", "a " })
            foreach (var left in units)
                foreach (var right in units)
                    CheckEveryLimit(prefix + left + right + " x\r\ny");
    }

    private static void CheckEveryLimit(string text)
    {
        var complete = new List<LineBreak>();
        var iterator = new LineBreakEnumerator(text);
        while (iterator.MoveNext(out var lineBreak))
            complete.Add(lineBreak);

        for (var limit = 0; limit <= text.Length; limit++)
        {
            var bounded = new List<LineBreak>();
            iterator = new LineBreakEnumerator(text);
            while (iterator.MoveNext(out var lineBreak, limit))
            {
                if (lineBreak.PositionMeasure > limit)
                    break;
                bounded.Add(lineBreak);
            }

            Assert.True(complete.TakeWhile(b => b.PositionMeasure <= limit).SequenceEqual(bounded),
                $"Limit {limit}; UTF-16 {string.Join(",", text.Select(c => ((int)c).ToString("X4")))}");
        }
    }
}
