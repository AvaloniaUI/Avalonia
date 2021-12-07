using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextLayoutTests
    {
        private const string SingleLineText = "0123456789";
        private const string MultiLineText = "01 23 45 678\r\rabc def gh ij";

        [InlineData("01234\r01234\r", 3)]
        [InlineData("01234\r01234", 2)]
        [Theory]
        public void Should_Break_Lines(string text, int numberOfLines)
        {
            using (Start())
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black);

                Assert.Equal(numberOfLines, layout.TextLines.Count);
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Text_In_Between()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new ValueSpan<TextRunProperties>(1, 2,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                };

                var layout = new TextLayout(
                    MultiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides: spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(3, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.Text.Length);

                var actual = textRun.Text.Buffer.Span.ToString();

                Assert.Equal("1 ", actual);

                Assert.Equal(foreground, textRun.Properties.ForegroundBrush);
            }
        }

        [Fact]
        public void Should_Not_Alter_Lines_After_TextStyleSpan_Was_Applied()
        {
            using (Start())
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var expected = new TextLayout(
                    MultiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textWrapping: TextWrapping.Wrap,
                    maxWidth: 25);
                
                var expectedLines = expected.TextLines.Select(x => MultiLineText.Substring(x.TextRange.Start,
                    x.TextRange.Length)).ToList();
                
                for (var i = 4; i < MultiLineText.Length; i++)
                {
                    var spans = new[]
                    {
                        new ValueSpan<TextRunProperties>(0, i,
                            new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                    };

                    var actual = new TextLayout(
                        MultiLineText,
                        Typeface.Default,
                        12.0f,
                        Brushes.Black.ToImmutable(),
                        textWrapping: TextWrapping.Wrap,
                        maxWidth: 25,
                        textStyleOverrides: spans);

                    var actualLines = actual.TextLines.Select(x => MultiLineText.Substring(x.TextRange.Start,
                        x.TextRange.Length)).ToList();
                    
                    Assert.Equal(expectedLines.Count, actualLines.Count);

                    for (var j = 0; j < actual.TextLines.Count; j++)
                    {
                        var expectedText = expectedLines[j];
                        
                        var actualText = actualLines[j];

                        Assert.Equal(expectedText, actualText);
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
                    new ValueSpan<TextRunProperties>(0, 2,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                };

                var layout = new TextLayout(
                    SingleLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides: spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(2, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(2, textRun.Text.Length);

                var actual = SingleLineText.Substring(textRun.Text.Start,
                    textRun.Text.Length);

                Assert.Equal("01", actual);

                Assert.Equal(foreground, textRun.Properties.ForegroundBrush);
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
                    new ValueSpan<TextRunProperties>(8, 2,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground)),
                };

                var layout = new TextLayout(
                    SingleLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides: spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(2, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.Text.Length);

                var actual = textRun.Text.Buffer.Span.ToString();

                Assert.Equal("89", actual);

                Assert.Equal(foreground, textRun.Properties.ForegroundBrush);
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
                    new ValueSpan<TextRunProperties>(0, 1,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                };

                var layout = new TextLayout(
                    "0",
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textStyleOverrides: spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(1, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(1, textRun.Text.Length);

                Assert.Equal(foreground, textRun.Properties.ForegroundBrush);
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
                    new ValueSpan<TextRunProperties>(2, 2,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
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

                Assert.Equal(foreground, textRun.Properties.ForegroundBrush);
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextLine_Length_Sum()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    MultiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                Assert.Equal(MultiLineText.Length, layout.TextLines.Sum(x => x.TextRange.Length));
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextRun_TextLength_Sum()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    MultiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                Assert.Equal(
                    MultiLineText.Length,
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
                    "Multiline TextBox with TextWrapping.\r\rLorem ipsum dolor sit amet";

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 24,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                };

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textWrapping: TextWrapping.Wrap,
                    maxWidth: 180,
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
                    new ValueSpan<TextRunProperties>(5, 20,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                };

                var layout = new TextLayout(
                    MultiLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    maxWidth: 200,
                    maxHeight: 125,
                    textStyleOverrides: spans);

                Assert.Equal(foreground, layout.TextLines[0].TextRuns[1].Properties.ForegroundBrush);
                Assert.Equal(foreground, layout.TextLines[1].TextRuns[0].Properties.ForegroundBrush);
                Assert.Equal(foreground, layout.TextLines[2].TextRuns[0].Properties.ForegroundBrush);
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

                var shapedRun = (ShapedTextCharacters)layout.TextLines[0].TextRuns[0];

                var glyphRun = shapedRun.GlyphRun;

                var width = glyphRun.Size.Width;

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
                    var shapedRun = (ShapedTextCharacters)textRun;

                    var glyphRun = shapedRun.GlyphRun;

                    var glyphClusters = glyphRun.GlyphClusters;

                    var expected = clusters.Skip(index).Take(glyphClusters.Length).ToArray();

                    Assert.Equal(expected, glyphRun.GlyphClusters);

                    index += glyphClusters.Length;
                }
            }
        }

        [Theory]
        [InlineData("abcde\r\n", 7)] // Carriage Return + Line Feed
        [InlineData("abcde\u000A", 6)] // Line Feed
        [InlineData("abcde\u000B", 6)] // Vertical Tab
        [InlineData("abcde\u000C", 6)] // Form Feed
        [InlineData("abcde\u000D", 6)] // Carriage Return
        public void Should_Break_With_BreakChar(string text, int expectedLength)
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

                Assert.Equal(expectedLength, ((ShapedTextCharacters)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphClusters.Length);

                Assert.Equal(5, ((ShapedTextCharacters)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphClusters[5]);

                if (expectedLength == 7)
                {
                    Assert.Equal(5, ((ShapedTextCharacters)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphClusters[6]);
                }
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

                var textRun = (ShapedTextCharacters)textLine.TextRuns[0];

                Assert.Equal(7, textRun.Text.Length);

                var replacementGlyph = Typeface.Default.GlyphTypeface.GetGlyph(Codepoint.ReplacementCodepoint);

                foreach (var glyph in textRun.GlyphRun.GlyphIndices)
                {
                    Assert.Equal(replacementGlyph, glyph);
                }
            }
        }

        [InlineData("0123456789\r0123456789")]
        [InlineData("0123456789")]
        [Theory]
        public void Should_Include_First_Line_When_Constraint_Is_Surpassed(string text)
        {
            using (Start())
            {
                var glyphTypeface = Typeface.Default.GlyphTypeface;

                var emHeight = glyphTypeface.DesignEmHeight;

                var lineHeight = (glyphTypeface.Descent - glyphTypeface.Ascent) * (12.0 / emHeight);

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12,
                    Brushes.Black.ToImmutable(),
                    maxHeight: lineHeight - lineHeight * 0.5);

                Assert.Equal(1, layout.TextLines.Count);

                Assert.Equal(lineHeight, layout.Size.Height);
            }
        }

        [InlineData("0123456789\r\n0123456789\r\n0123456789", 0, 3)]
        [InlineData("0123456789\r\n0123456789\r\n0123456789", 1, 1)]
        [InlineData("0123456789\r\n0123456789\r\n0123456789", 4, 3)]
        [Theory]
        public void Should_Not_Exceed_MaxLines(string text, int maxLines, int expectedLines)
        {
            using (Start())
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12,
                    Brushes.Black,
                    maxWidth: 50,
                    maxLines: maxLines);

                Assert.Equal(expectedLines, layout.TextLines.Count);
            }
        }

        [Fact]
        public void Should_Produce_Fixed_Height_Lines()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    MultiLineText,
                    Typeface.Default,
                    12,
                    Brushes.Black,
                    lineHeight: 50);

                foreach (var line in layout.TextLines)
                {
                    Assert.Equal(50, line.Height);
                }
            }
        }

        private const string Text = "日本でTest一番読まれている英字新聞・ジャパンタイムズが発信する国内外ニュースと、様々なジャンルの特集記事。";

        [Fact(Skip = "Only used for profiling.")]
        public void Should_Wrap()
        {
            using (Start())
            {
                for (var i = 0; i < 2000; i++)
                {
                    var layout = new TextLayout(
                        Text,
                        Typeface.Default,
                        12,
                        Brushes.Black,
                        textWrapping: TextWrapping.Wrap,
                        maxWidth: 50);
                }
            }
        }

        [Fact]
        public void Should_Process_Multiple_NewLines_Properly()
        {
            using (Start())
            {
                var text = "123\r\n\r\n456\r\n\r\n";
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black);

                Assert.Equal(5, layout.TextLines.Count);

                Assert.Equal("123\r\n", layout.TextLines[0].TextRuns[0].Text);
                Assert.Equal("\r\n", layout.TextLines[1].TextRuns[0].Text);
                Assert.Equal("456\r\n", layout.TextLines[2].TextRuns[0].Text);
                Assert.Equal("\r\n", layout.TextLines[3].TextRuns[0].Text);
            }
        }

        [Fact]
        public void Should_Wrap_Min_OneCharacter_EveryLine()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    SingleLineText,
                    Typeface.Default,
                    12,
                    Brushes.Black,
                    textWrapping: TextWrapping.Wrap,
                    maxWidth: 3);

                //every character should be new line as there not enough space for even one character
                Assert.Equal(SingleLineText.Length, layout.TextLines.Count);
            }
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    textShaperImpl: new TextShaperImpl(),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
