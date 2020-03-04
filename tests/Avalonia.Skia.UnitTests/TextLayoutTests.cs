using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class TextLayoutTests
    {
        private static readonly string s_singleLineText = "0123456789";
        private static readonly string s_multiLineText = "012345678\r\r0123456789";

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Text_In_Between()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(1, 2),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default, 
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides : spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(3, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.Text.Length);

                var actual = textRun.Text.Buffer.Span.ToString();

                Assert.Equal("12", actual);

                Assert.Equal(foreground, textRun.Style.Foreground);
            }
        }

        [Fact]
        public void Should_Not_Alter_Lines_After_TextStyleSpan_Was_Applied()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                for (var i = 4; i < s_multiLineText.Length; i++)
                {
                    var spans = new[]
                    {
                        new TextStyleRun(
                            new TextPointer(0, i),
                            new TextStyle(Typeface.Default, 12, foreground))
                    };

                    var expected = new TextLayout(
                        s_multiLineText,
                        Typeface.Default,
                        12.0f,
                        Brushes.Black.ToImmutable(),
                        textWrapping: TextWrapping.Wrap,
                        maxWidth : 25);

                    var actual = new TextLayout(
                        s_multiLineText,
                        Typeface.Default,
                        12.0f,
                        Brushes.Black.ToImmutable(),
                        textWrapping : TextWrapping.Wrap,
                        maxWidth : 25,
                        textStyleOverrides : spans);

                    Assert.Equal(expected.TextLines.Count, actual.TextLines.Count);

                    for (var j = 0; j < actual.TextLines.Count; j++)
                    {
                        Assert.Equal(expected.TextLines[j].Text.Length, actual.TextLines[j].Text.Length);

                        Assert.Equal(expected.TextLines[j].TextRuns.Sum(x => x.Text.Length),
                            actual.TextLines[j].TextRuns.Sum(x => x.Text.Length));
                    }
                }
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Text_At_Start()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(0, 2),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    s_singleLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides : spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(2, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(2, textRun.Text.Length);

                var actual = s_singleLineText.Substring(textRun.Text.Start,
                    textRun.Text.Length);

                Assert.Equal("01", actual);

                Assert.Equal(foreground, textRun.Style.Foreground);
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Text_At_End()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(8, 2),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    s_singleLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides : spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(2, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.Text.Length);

                var actual = textRun.Text.Buffer.Span.ToString();

                Assert.Equal("89", actual);

                Assert.Equal(foreground, textRun.Style.Foreground);
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Single_Character()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(0, 1),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    "0",
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides : spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(1, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(1, textRun.Text.Length);

                Assert.Equal(foreground, textRun.Style.Foreground);
            }
        }

        [Fact]
        public void Should_Apply_TextSpan_To_Unicode_String_In_Between()
        {
            using (Start())
            {
                const string text = "😄😄😄😄";

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(2, 2),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides: spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(3, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.Text.Length);

                var actual = textRun.Text.Buffer.Span.ToString();

                Assert.Equal("😄", actual);

                Assert.Equal(foreground, textRun.Style.Foreground);
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextLine_Length_Sum()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                Assert.Equal(s_multiLineText.Length, layout.TextLines.Sum(x => x.Text.Length));
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextRun_TextLength_Sum()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                Assert.Equal(
                    s_multiLineText.Length,
                    layout.TextLines.Select(textLine =>
                            textLine.TextRuns.Sum(textRun => textRun.Text.Length))
                        .Sum());
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextRun_TextLength_Sum_After_Wrap_With_Style_Applied()
        {
            using (Start())
            {
                const string text =
                    "Multiline TextBox with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. " +
                    "Curabitur massa. Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus pretium ornare est.";

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(0, 24),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textWrapping : TextWrapping.Wrap,
                    maxWidth : 180,
                    textStyleOverrides: spans);

                Assert.Equal(
                    text.Length,
                    layout.TextLines.Select(textLine =>
                            textLine.TextRuns.Sum(textRun => textRun.Text.Length))
                        .Sum());
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_MultiLine()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new TextStyleRun(
                        new TextPointer(5, 20),
                        new TextStyle(Typeface.Default, 12, foreground))
                };

                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    maxWidth : 200, 
                    maxHeight : 125,
                    textStyleOverrides: spans);

                Assert.Equal(foreground, layout.TextLines[0].TextRuns[1].Style.Foreground);
                Assert.Equal(foreground, layout.TextLines[1].TextRuns[0].Style.Foreground);
                Assert.Equal(foreground, layout.TextLines[2].TextRuns[0].Style.Foreground);
            }
        }

        [Fact]
        public void Should_Hit_Test_SurrogatePair()
        {
            using (Start())
            {
                const string text = "😄😄";

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                var shapedRun = (ShapedTextRun)layout.TextLines[0].TextRuns[0];

                var glyphRun = shapedRun.GlyphRun;

                var width = glyphRun.Bounds.Width;

                var characterHit = glyphRun.GetCharacterHitFromDistance(width, out _);

                Assert.Equal(2, characterHit.FirstCharacterIndex);

                Assert.Equal(2, characterHit.TrailingLength);
            }
        }


        [Theory]
        [InlineData("☝🏿", new ushort[] { 0 })]
        [InlineData("☝🏿 ab", new ushort[] { 0, 3, 4, 5 })]
        [InlineData("ab ☝🏿", new ushort[] { 0, 1, 2, 3 })]
        public void Should_Create_Valid_Clusters_For_Text(string text, ushort[] clusters)
        {
            using (Start())
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                var textLine = layout.TextLines[0];

                var index = 0;

                foreach (var textRun in textLine.TextRuns)
                {
                    var shapedRun = (ShapedTextRun)textRun;

                    var glyphRun = shapedRun.GlyphRun;

                    var glyphClusters = glyphRun.GlyphClusters;

                    var expected = clusters.Skip(index).Take(glyphClusters.Length).ToArray();

                    Assert.Equal(expected, glyphRun.GlyphClusters);

                    index += glyphClusters.Length;
                }
            }
        }

        [Theory]
        [InlineData("abcde\r\n")]
        [InlineData("abcde\n\r")]
        public void Should_Break_With_BreakChar_Pair(string text)
        {
            using (Start())
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                Assert.Equal(2, layout.TextLines.Count);

                Assert.Equal(1, layout.TextLines[0].TextRuns.Count);

                Assert.Equal(7, ((ShapedTextRun)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphClusters.Length);

                Assert.Equal(5, ((ShapedTextRun)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphClusters[5]);

                Assert.Equal(5, ((ShapedTextRun)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphClusters[6]);
            }
        }

        [Fact]
        public void Should_Have_One_Run_With_Common_Script()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    "abcde\r\n",
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                Assert.Equal(1, layout.TextLines[0].TextRuns.Count);
            }
        }

        [Fact]
        public void Should_Layout_Corrupted_Text()
        {
            using (Start())
            {
                var text = new string(new[] { '\uD802', '\uD802', '\uD802', '\uD802', '\uD802', '\uD802', '\uD802' });

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12,
                    Brushes.Black.ToImmutable());

                var textLine = layout.TextLines[0];

                var textRun = (ShapedTextRun)textLine.TextRuns[0];

                Assert.Equal(7, textRun.Text.Length);

                var replacementGlyph = Typeface.Default.GlyphTypeface.GetGlyph(Codepoint.ReplacementCodepoint);

                foreach (var glyph in textRun.GlyphRun.GlyphIndices)
                {
                    Assert.Equal(replacementGlyph, glyph);
                }
            }
        }

        public static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null), 
                    textShaperImpl: new TextShaperImpl(),
                    fontManagerImpl : new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
