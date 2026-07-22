using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Headless;
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
        private const string RightToLeftText = "זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן";

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

                Assert.Equal(2, textRun.Length);

                var actual = textRun.Text.ToString();

                Assert.Equal("1 ", actual);

                Assert.NotNull(textRun.Properties);
                Assert.Equal(foreground, textRun.Properties.ForegroundBrush);
            }
        }

        [InlineData(27)]
        [InlineData(22)]
        [Theory]
        public void Should_Wrap_And_Apply_Style(int length)
        {
            using (Start())
            {
                var text = "Multiline TextBox with TextWrapping.";

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var expected = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textWrapping: TextWrapping.Wrap,
                    maxWidth: 200);

                var expectedLines = expected.TextLines.Select(x => text.Substring(x.FirstTextSourceIndex,
                    x.Length)).ToList();

                var spans = new[]
                {
                    new ValueSpan<TextRunProperties>(0, length,
                        new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                };

                var actual = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable(),
                    textWrapping: TextWrapping.Wrap,
                    maxWidth: 200,
                    textStyleOverrides: spans);

                var actualLines = actual.TextLines.Select(x => text.Substring(x.FirstTextSourceIndex,
                    x.Length)).ToList();

                Assert.Equal(expectedLines.Count, actualLines.Count);

                for (var j = 0; j < actual.TextLines.Count; j++)
                {
                    var expectedText = expectedLines[j];

                    var actualText = actualLines[j];

                    Assert.Equal(expectedText, actualText);
                }

            }
        }

        [Fact]
        public void Should_Not_Alter_Lines_After_TextStyleSpan_Was_Applied()
        {
            using (Start())
            {
                const string text = "אחד !\ntwo !\nשְׁלוֹשָׁה !";

                var red = new SolidColorBrush(Colors.Red).ToImmutable();
                var black = Brushes.Black.ToImmutable();

                var expected = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    black,
                    textWrapping: TextWrapping.Wrap);

                var expectedGlyphs = GetGlyphs(expected);

                var outer = new GraphemeEnumerator(text);
                var inner = new GraphemeEnumerator(text);
                var i = 0;
                var j = 0;

                while (true)
                {
                    Grapheme grapheme;
                    while (inner.MoveNext(out grapheme))
                    {
                        j += grapheme.Length;

                        if (j + i > text.Length)
                        {
                            break;
                        }

                        var spans = new[]
                        {
                            new ValueSpan<TextRunProperties>(i, j,
                                new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: red))
                        };

                        var actual = new TextLayout(
                            text,
                            Typeface.Default,
                            12.0f,
                            black,
                            textWrapping: TextWrapping.Wrap,
                            textStyleOverrides: spans);

                        var actualGlyphs = GetGlyphs(actual);

                        Assert.Equal(expectedGlyphs.Count, actualGlyphs.Count);

                        for (var k = 0; k < expectedGlyphs.Count; k++)
                        {
                            Assert.Equal(expectedGlyphs[k], actualGlyphs[k]);
                        }
                    }

                    if (!outer.MoveNext(out grapheme))
                    {
                        break;
                    }

                    inner = new GraphemeEnumerator(text);

                    i += grapheme.Length;
                }
            }

            static List<string> GetGlyphs(TextLayout textLayout)
                => textLayout.TextLines
                    .Select(line => string.Join('|', line.TextRuns
                        .Cast<ShapedTextRun>()
                        .SelectMany(run => run.ShapedBuffer, (_, glyph) => glyph.GlyphIndex)))
                    .ToList();
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

                Assert.Equal(2, textRun.Length);

                var actual = SingleLineText[..textRun.Length];

                Assert.Equal("01", actual);

                Assert.NotNull(textRun.Properties);
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

                Assert.Equal(2, textRun.Length);

                var actual = textRun.Text.ToString();

                Assert.Equal("89", actual);

                Assert.NotNull(textRun.Properties);
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

                Assert.Equal(1, textRun.Length);

                Assert.NotNull(textRun.Properties);
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

                Assert.Equal(2, textRun.Length);

                var actual = textRun.Text.ToString();

                Assert.Equal("😄", actual);

                Assert.NotNull(textRun.Properties);
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

                Assert.Equal(MultiLineText.Length, layout.TextLines.Sum(x => x.Length));
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
                            textLine.TextRuns.Sum(textRun => textRun.Length))
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
                            textLine.TextRuns.Sum(textRun => textRun.Length))
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

                Assert.Equal(foreground, layout.TextLines[0].TextRuns[1].Properties?.ForegroundBrush);
                Assert.Equal(foreground, layout.TextLines[1].TextRuns[0].Properties?.ForegroundBrush);
                Assert.Equal(foreground, layout.TextLines[2].TextRuns[0].Properties?.ForegroundBrush);
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
        [InlineData("☝🏿", new int[] { 0 })]
        [InlineData("☝🏿 ab", new int[] { 0, 3, 0, 1 })]
        [InlineData("ab ☝🏿", new int[] { 0, 1, 2, 0 })]
        public void Should_Create_Valid_Clusters_For_Text(string text, int[] clusters)
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

                    var glyphClusters = shapedRun.ShapedBuffer.Select(glyph => glyph.GlyphCluster).ToArray();

                    var expected = clusters.Skip(index).Take(glyphClusters.Length).ToArray();

                    Assert.Equal(expected, glyphClusters);

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

                Assert.Equal(expectedLength, ((ShapedTextRun)layout.TextLines[0].TextRuns[0]).GlyphRun.GlyphInfos.Count);

                Assert.Equal(5, ((ShapedTextRun)layout.TextLines[0].TextRuns[0]).ShapedBuffer[5].GlyphCluster);

                if (expectedLength == 7)
                {
                    Assert.Equal(5, ((ShapedTextRun)layout.TextLines[0].TextRuns[0]).ShapedBuffer[6].GlyphCluster);
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

                var textRun = (ShapedTextRun)textLine.TextRuns[0];

                Assert.Equal(7, textRun.Length);

                var replacementGlyph = Typeface.Default.GlyphTypeface.CharacterToGlyphMap[Codepoint.ReplacementCodepoint];

                foreach (var glyphInfo in textRun.GlyphRun.GlyphInfos)
                {
                    Assert.Equal(replacementGlyph, glyphInfo.GlyphIndex);
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

                var emHeight = glyphTypeface.Metrics.DesignEmHeight;

                var lineHeight = glyphTypeface.Metrics.LineSpacing * (12.0 / emHeight);

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12,
                    Brushes.Black.ToImmutable(),
                    maxHeight: lineHeight - lineHeight * 0.5);

                Assert.Equal(1, layout.TextLines.Count);

                Assert.Equal(lineHeight, layout.Height);
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

                Assert.Equal("123\r\n", layout.TextLines[0].TextRuns[0].Text.ToString());
                Assert.Equal("\r\n", layout.TextLines[1].TextRuns[0].Text.ToString());
                Assert.Equal("456\r\n", layout.TextLines[2].TextRuns[0].Text.ToString());
                Assert.Equal("\r\n", layout.TextLines[3].TextRuns[0].Text.ToString());
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

        [Fact]
        public void Should_HitTestTextRange_RightToLeft()
        {
            using (Start())
            {
                const int start = 0;
                const int length = 10;

                var layout = new TextLayout(
                    RightToLeftText,
                    Typeface.Default,
                    12,
                    Brushes.Black);

                var selectedText = new TextLayout(
                    RightToLeftText.Substring(start, length),
                    Typeface.Default,
                    12,
                    Brushes.Black);

                var rects = layout.HitTestTextRange(start, length).ToArray();

                Assert.Equal(1, rects.Length);

                var selectedRect = rects[0];

                Assert.Equal(selectedText.WidthIncludingTrailingWhitespace, selectedRect.Width, 2);
            }
        }

        [Fact]
        public void Should_HitTestTextRange_BiDi()
        {
            const string text = "זה כיףabcDEFזה כיף";

            using (Start())
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                var textLine = layout.TextLines[0];

                var start = textLine.GetDistanceFromCharacterHit(new CharacterHit(5, 1));

                var end = textLine.GetDistanceFromCharacterHit(new CharacterHit(6, 1));

                var rects = layout.HitTestTextRange(0, 7).ToArray();

                Assert.Equal(1, rects.Length);

                var expected = rects[0];

                Assert.Equal(expected.Left, start);
                Assert.Equal(expected.Right, end);
            }
        }

        [Fact]
        public void Should_HitTestTextRange()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    SingleLineText,
                    Typeface.Default,
                    12.0f,
                    Brushes.Black.ToImmutable());

                var lineRects = layout.HitTestTextRange(0, SingleLineText.Length).ToList();

                Assert.Equal(layout.TextLines.Count, lineRects.Count);

                for (var i = 0; i < layout.TextLines.Count; i++)
                {
                    var textLine = layout.TextLines[i];
                    var rect = lineRects[i];

                    Assert.Equal(textLine.WidthIncludingTrailingWhitespace, rect.Width);
                }

                var rects = layout.TextLines
                    .SelectMany(x => x.TextRuns.Cast<ShapedTextRun>())
                    .SelectMany(x => x.ShapedBuffer, (_, glyph) => glyph.GlyphAdvance)
                    .ToArray();

                for (var i = 0; i < SingleLineText.Length; i++)
                {
                    for (var j = 1; i + j < SingleLineText.Length; j++)
                    {
                        var expected = rects.AsSpan(i, j).ToArray().Sum();
                        var actual = layout.HitTestTextRange(i, j).Sum(x => x.Width);

                        Assert.Equal(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void Should_Wrap_RightToLeft()
        {
            const string text =
                "يَجِبُ عَلَى الإنْسَانِ أن يَكُونَ أمِيْنَاً وَصَادِقَاً مَعَ نَفْسِهِ وَمَعَ أَهْلِهِ وَجِيْرَانِهِ وَأَنْ يَبْذُلَ كُلَّ جُهْدٍ فِي إِعْلاءِ شَأْنِ الوَطَنِ وَأَنْ يَعْمَلَ عَلَى مَا يَجْلِبُ السَّعَادَةَ لِلنَّاسِ . ولَن يَتِمَّ لَهُ ذلِك إِلا بِأَنْ يُقَدِّمَ المَنْفَعَةَ العَامَّةَ عَلَى المَنْفَعَةِ الخَاصَّةِ وَهذَا مِثَالٌ لِلتَّضْحِيَةِ .";

            using (Start())
            {
                for (var maxWidth = 366; maxWidth < 900; maxWidth += 33)
                {
                    var layout = new TextLayout(
                        text,
                        Typeface.Default,
                        12.0f,
                        Brushes.Black.ToImmutable(),
                        textWrapping: TextWrapping.Wrap,
                        flowDirection: FlowDirection.RightToLeft,
                        maxWidth: maxWidth);

                    foreach (var textLine in layout.TextLines)
                    {
                        Assert.True(textLine.Width <= maxWidth);

                        var actual = new string(textLine.TextRuns.Cast<ShapedTextRun>()
                            .OrderBy(x => TextTestHelper.GetStartCharIndex(x.Text))
                            .SelectMany(x => x.Text.ToString())
                            .ToArray());

                        var expected = text.Substring(textLine.FirstTextSourceIndex, textLine.Length);

                        Assert.Equal(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void Should_Layout_Empty_String()
        {
            using (Start())
            {
                var layout = new TextLayout(
                    string.Empty,
                    Typeface.Default,
                    12,
                    Brushes.Black);

                Assert.True(layout.Height > 0);
            }
        }

        [Fact]
        public void Should_HitTestPoint_RightToLeft()
        {
            using (Start())
            {
                var text = "אאא AAA";

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12,
                    Brushes.Black,
                    flowDirection: FlowDirection.RightToLeft);

                var firstRun = Assert.IsType<ShapedTextRun>(layout.TextLines[0].TextRuns[0]);

                var hit = layout.HitTestPoint(new Point());

                Assert.Equal(4, hit.TextPosition);

                var firstRunOffset = TextTestHelper.GetStartCharIndex(firstRun.Text);
                var currentX = 0.0;

                for (var i = 0; i < firstRun.GlyphRun.GlyphInfos.Count; i++)
                {
                    var cluster = firstRun.GlyphRun.GlyphInfos[i].GlyphCluster + firstRunOffset;
                    var advance = firstRun.GlyphRun.GlyphInfos[i].GlyphAdvance;

                    hit = layout.HitTestPoint(new Point(currentX, 0));

                    Assert.Equal(cluster, hit.TextPosition);

                    var hitRange = layout.HitTestTextRange(hit.TextPosition, 1);

                    var distance = hitRange.First().Left;

                    Assert.Equal(currentX, distance, 2);

                    currentX += advance;
                }

                var secondRun = Assert.IsType<ShapedTextRun>(layout.TextLines[0].TextRuns[1]);

                hit = layout.HitTestPoint(new Point(firstRun.Size.Width, 0));

                Assert.Equal(7, hit.TextPosition);

                hit = layout.HitTestPoint(new Point(layout.TextLines[0].WidthIncludingTrailingWhitespace, 0));

                Assert.Equal(0, hit.TextPosition);

                var secondRunOffset = TextTestHelper.GetStartCharIndex(secondRun.Text);
                currentX = firstRun.Size.Width + 0.5;

                for (var i = 0; i < secondRun.GlyphRun.GlyphInfos.Count; i++)
                {
                    var cluster = secondRun.GlyphRun.GlyphInfos[i].GlyphCluster + secondRunOffset;
                    var advance = secondRun.GlyphRun.GlyphInfos[i].GlyphAdvance;

                    hit = layout.HitTestPoint(new Point(currentX, 0));

                    Assert.Equal(cluster, hit.CharacterHit.FirstCharacterIndex);

                    var hitRange = layout.HitTestTextRange(hit.CharacterHit.FirstCharacterIndex, hit.CharacterHit.TrailingLength);

                    var distance = hitRange.First().Left + 0.5;

                    Assert.Equal(currentX, distance, 2);

                    currentX += advance;
                }
            }
        }

        [Fact]
        public void Should_Get_CharacterHit_From_Distance_RTL()
        {
            using (Start())
            {
                var text = "أَبْجَدِيَّة عَرَبِيَّة";

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12,
                    Brushes.Black);

                var textLine = layout.TextLines[0];

                var firstRun = (ShapedTextRun)textLine.TextRuns[0];

                var firstCluster = firstRun.ShapedBuffer[0].GlyphCluster;

                var characterHit = textLine.GetCharacterHitFromDistance(0);

                Assert.Equal(firstCluster, characterHit.FirstCharacterIndex);

                Assert.Equal(text.Length, characterHit.FirstCharacterIndex + characterHit.TrailingLength);

                var distance = textLine.GetDistanceFromCharacterHit(characterHit);

                Assert.Equal(0, distance);

                distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(characterHit.FirstCharacterIndex));

                var firstAdvance = firstRun.ShapedBuffer[0].GlyphAdvance;

                Assert.Equal(firstAdvance, distance, 5);

                var rect = layout.HitTestTextPosition(22);

                Assert.Equal(firstAdvance, rect.Left, 5);

                rect = layout.HitTestTextPosition(23);

                Assert.Equal(0, rect.Left, 5);

            }
        }

        [Fact]
        public void Should_Get_CharacterHit_From_Distance_RTL_With_TextStyles()
        {
            using (Start())
            {
                var text = "أَبْجَدِيَّة عَرَبِيَّة";

                var i = 0;

                var graphemeEnumerator = new GraphemeEnumerator(text);

                while (graphemeEnumerator.MoveNext(out var grapheme))
                {
                    var textStyleOverrides = new[] { new ValueSpan<TextRunProperties>(i, grapheme.Length, new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Red)) };

                    i += grapheme.Length;

                    var layout = new TextLayout(
                        text,
                        Typeface.Default,
                        12,
                        Brushes.Black,
                        textStyleOverrides: textStyleOverrides);

                    var textLine = layout.TextLines[0];

                    var shapedRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

                    var runStarts = textLine
                        .GetTextBounds(textLine.FirstTextSourceIndex, textLine.Length)
                        .SelectMany(bounds => bounds.TextRunBounds)
                        .Where(bounds => bounds.TextRun is ShapedTextRun)
                        .ToDictionary(bounds => (ShapedTextRun)bounds.TextRun, bounds => bounds.TextSourceCharacterIndex);

                    var clusters = shapedRuns.SelectMany(run =>
                    {
                        var rawClusters = run.ShapedBuffer.Select(glyph => glyph.GlyphCluster).ToList();

                        if (!runStarts.TryGetValue(run, out var runStart) || rawClusters.Count == 0)
                        {
                            return rawClusters;
                        }

                        // Clusters can be either run-local or text-source relative depending on split history.
                        if (rawClusters.Min() < runStart)
                        {
                            return rawClusters.Select(cluster => cluster + runStart);
                        }

                        return rawClusters;
                    }).ToList();

                    var glyphAdvances = shapedRuns.SelectMany(x => x.ShapedBuffer, (_, glyph) => glyph.GlyphAdvance).ToList();

                    var currentX = 0.0;

                    var cluster = text.Length;

                    for (int j = 0; j < clusters.Count - 1; j++)
                    {                     
                        var glyphAdvance = glyphAdvances[j];

                        var characterHit = textLine.GetCharacterHitFromDistance(currentX);

                        Assert.True(cluster == characterHit.FirstCharacterIndex + characterHit.TrailingLength,
                            $"grapheme={i - grapheme.Length}, j={j}, cluster={cluster}, hit={characterHit.FirstCharacterIndex}+{characterHit.TrailingLength}, currentX={currentX}, textLen={text.Length}, runs={shapedRuns.Count}, clusters=[{string.Join(",", clusters)}]");

                        var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

                        Assert.Equal(currentX, distance, 5);

                        currentX += glyphAdvance;

                        if(glyphAdvance > 0)
                        {
                            cluster = clusters[j];
                        }
                    }
                }
            }
        }

        [InlineData("mgfg🧐df f sdf", "g🧐d", 20, 40)]
        [InlineData("وه. وقد تعرض لانتقادات", "دات", 5, 30)]
        [InlineData("وه. وقد تعرض لانتقادات", "تعرض", 20, 50)]
        [InlineData(" علمية 😱ومضللة ،", " علمية 😱ومضللة ،", 40, 100)]
        [InlineData("في عام 2018 ، رفعت ل", "في عام 2018 ، رفعت ل", 100, 120)]
        [Theory]
        public void HitTestTextRange_Range_ValidLength(string text, string textToSelect, double minWidth, double maxWidth)
        {
            using (Start())
            {
                var layout = new TextLayout(text, Typeface.Default, 12, Brushes.Black);
                var start = text.IndexOf(textToSelect);
                var selectionRectangles = layout.HitTestTextRange(start, textToSelect.Length);
                Assert.Equal(1, selectionRectangles.Count());
                var rect = selectionRectangles.First();
                Assert.InRange(rect.Width, minWidth, maxWidth);
            }
        }

        [InlineData("012🧐210", 2, 4, FlowDirection.LeftToRight, "14.40234375,40.8046875")]
        [InlineData("210🧐012", 2, 4, FlowDirection.RightToLeft, "0,7.201171875;21.603515625,33.603515625;48.005859375,55.20703125")]
        [InlineData("שנב🧐שנב", 2, 4, FlowDirection.LeftToRight, "11.268,38.208")]
        [InlineData("שנב🧐שנב", 2, 4, FlowDirection.RightToLeft, "11.268,38.208")]
        [Theory]
        public void Should_HitTestTextRangeBetweenRuns(string text, int start, int length, 
            FlowDirection flowDirection, string expected)
        {
            using (Start())
            {
                var expectedRects = expected.Split(';').Select(x =>
                {
                    var startEnd = x.Split(',');

                    var start = double.Parse(startEnd[0], CultureInfo.InvariantCulture);

                    var end = double.Parse(startEnd[1], CultureInfo.InvariantCulture);

                    return new Rect(start, 0, end - start, 0);
                }).ToArray();

                var textLayout = new TextLayout(text, Typeface.Default, 12, Brushes.Black, flowDirection: flowDirection);

                var rects = textLayout.HitTestTextRange(start, length).ToArray();

                Assert.Equal(expectedRects.Length, rects.Length);

                var endX = textLayout.TextLines[0].GetDistanceFromCharacterHit(new CharacterHit(2));
                var startX = textLayout.TextLines[0].GetDistanceFromCharacterHit(new CharacterHit(5, 1));

                for (int i = 0; i < expectedRects.Length; i++)
                {
                    var expectedRect = expectedRects[i];

                    Assert.Equal(expectedRect.Left, rects[i].Left, 2);

                    Assert.Equal(expectedRect.Right, rects[i].Right, 2);
                }            
            }
        }

        [Fact]
        public void Should_HitTestTextRangeWithLineBreaks()
        {
            using (Start())
            {
                var beforeLinebreak = "Line before linebreak";
                var afterLinebreak = "Line after linebreak";
                var text = beforeLinebreak + Environment.NewLine + "" + Environment.NewLine + afterLinebreak;

                var textLayout = new TextLayout(text, Typeface.Default, 12, Brushes.Black);

                var end = text.Length - afterLinebreak.Length + 1;

                var rects = textLayout.HitTestTextRange(0, end).ToArray();

                Assert.Equal(3, rects.Length);

                var endX = textLayout.TextLines[2].GetDistanceFromCharacterHit(new CharacterHit(end));

                //First character should be covered
                Assert.Equal(7.201171875, endX, 2);
            }
        }

        [Fact]
        public void Should_HitTestTextPosition_EndOfLine_RTL()
        {
            var text = "גש\r\n";

            using (Start())
            {
                var textLayout = new TextLayout(text, Typeface.Default, 12, Brushes.Black, flowDirection: FlowDirection.RightToLeft);

                var rect = textLayout.HitTestTextPosition(text.Length);

                Assert.Equal(16.32, rect.Top);
            }
        }

        [Win32Fact("Font only available on Windows")]
        public void Should_Handle_TextStyle_With_Ligature()
        {
            using (Start())
            {
                var text = "fi";

                var typeface = new Typeface("Calibri");

                var textLayout = new TextLayout(text, typeface, 12, Brushes.Black,
                    textStyleOverrides: new[]
                    {
                        new ValueSpan<TextRunProperties>(1, 1,
                            new GenericTextRunProperties(typeface, foregroundBrush: Brushes.White))
                    });

                Assert.NotNull(textLayout);
            }
        }

        [Fact]
        public void Should_Measure_TextLayoutSymbolWithAndWidthIncludingTrailingWhitespace()
        {
            const string symbolsFont = "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Symbols";
            using (Start())
            {
                var textLayout = new TextLayout("\ue971", new Typeface(symbolsFont), 12.0, Brushes.White);

                Assert.Equal(new Size(12.0, 12.0), new Size(textLayout.Width, textLayout.Height));
                Assert.Equal(12.0, textLayout.WidthIncludingTrailingWhitespace);
            }
        }

        [Fact]
        public void Should_Wrap_With_LineEnd()
        {
            using (Start())
            {
                var defaultProperties =
                   new GenericTextRunProperties(Typeface.Default, 72, foregroundBrush: Brushes.Black);

                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap);

                var textLayout = new TextLayout(new SingleBufferTextSource("01", defaultProperties, true), paragraphProperties, maxWidth: 36);

                Assert.Equal(2, textLayout.TextLines.Count);

                var lastLine = textLayout.TextLines.Last();

                Assert.Equal(2, lastLine.TextRuns.Count);

                var lastRun = lastLine.TextRuns.Last();

                Assert.IsAssignableFrom<TextEndOfLine>(lastRun);
            }
        }

        [Fact]
        public void Should_Measure_TextLayoutSymbolWithAndWidthIncludingTrailingWhitespaceAndMinTextWidth()
        {
            using (Start())
            {
                const string monospaceFont = "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono";

                var typeFace = new Typeface(monospaceFont);

                var glyphTypeface = typeFace.GlyphTypeface;

                var textLayout0 = new TextLayout("aaaa", typeFace, 12.0, Brushes.White);
                Assert.Equal(textLayout0.WidthIncludingTrailingWhitespace, textLayout0.Width);

                var textLayout01 = new TextLayout("a a", typeFace, 12.0, Brushes.White);
                var textLayout1 = new TextLayout("a a ", typeFace, 12.0, Brushes.White);
                Assert.Equal(new Size(textLayout0.Width, textLayout0.Height), new Size(textLayout1.WidthIncludingTrailingWhitespace, textLayout1.Height));
                Assert.Equal(textLayout0.WidthIncludingTrailingWhitespace, textLayout1.WidthIncludingTrailingWhitespace);


                var textLayout2 = new TextLayout(" aa ", typeFace, 12.0, Brushes.White);
                Assert.Equal(new Size(textLayout1.Width, textLayout1.Height), new Size(textLayout2.Width, textLayout2.Height));
                Assert.Equal(textLayout0.WidthIncludingTrailingWhitespace, textLayout2.WidthIncludingTrailingWhitespace);
                Assert.Equal(textLayout01.Width, textLayout2.Width);
                
                var textLayout3 = new TextLayout("    ", typeFace, 12.0, Brushes.White);
                Assert.Equal(new Size(0, textLayout0.Height), new Size(textLayout3.Width, textLayout3.Height));
                Assert.Equal(textLayout0.WidthIncludingTrailingWhitespace, textLayout3.WidthIncludingTrailingWhitespace);
                Assert.Equal(0, textLayout3.Width);
            }
        }
        
        [Fact]
        public void InterWordJustification_Does_Not_Stretch_Last_CJK_Glyph()
        {
            using (Start())
            {
                // Pure CJK (Han) has no inter-word spaces; UAX#14 LB31 yields a break opportunity
                // between essentially every ideograph, so justification distributes space
                // inter-character. Justifying to a width wider than the shaped line must widen
                // interior glyphs (including the first) but leave the final visible glyph's advance
                // untouched - otherwise the last ideograph is not flush to the line edge. Drives
                // InterWordJustification directly with an explicit target width to avoid the
                // widest-line / last-line behaviour of the full TextLayout pipeline.
                const string text = "一二三四五";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var naturalWidth = textLine.WidthIncludingTrailingWhitespace;
                var before = GlyphAdvances(textLine);

                Assert.True(before.Count >= 2);

                var targetWidth = naturalWidth + 40;

                textLine.Justify(new InterWordJustification(targetWidth));

                var after = GlyphAdvances(textLine);

                Assert.Equal(before.Count, after.Count);

                // The line is stretched to the requested width.
                Assert.Equal(targetWidth, textLine.WidthIncludingTrailingWhitespace, 3);

                // The last visible glyph keeps its original advance (no trailing overshoot).
                Assert.Equal(before[before.Count - 1], after[after.Count - 1], 3);

                // The first glyph participates in justification (the leading gap is widened).
                AssertGreaterThan(after[0], before[0], "The first glyph should be widened");
            }
        }

        [Fact]
        public void Should_Justify_Wrapped_CJK_Line_To_MaxWidth()
        {
            using (Start())
            {
                // With the MaxWidth justification target (not the widest produced line), a wrapped
                // CJK paragraph fills each justified line to the paragraph margin, without stretching
                // the last visible glyph of the line. Compared against a Left layout of the same
                // text/width so the assertions do not depend on the fallback font's metrics.
                const string text = "一二三四五六七八九十一二三四五六七八九十";
                const double maxWidth = 100;

                var foreground = Brushes.Black.ToImmutable();

                var left = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Left, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                var justified = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Justify, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                // A wrapped, non-last line (the last line's treatment is a separate concern).
                Assert.True(justified.TextLines.Count >= 2);

                var leftLine = left.TextLines[0];
                var justifiedLine = justified.TextLines[0];

                // The line must have slack to distribute, otherwise the test proves nothing.
                AssertGreaterThan(maxWidth, leftLine.WidthIncludingTrailingWhitespace,
                    "The unjustified line must be narrower than MaxWidth");

                var before = GlyphAdvances(leftLine);
                var after = GlyphAdvances(justifiedLine);

                Assert.Equal(before.Count, after.Count);

                // The wrapped non-last line fills to MaxWidth (not the widest produced line).
                Assert.Equal(maxWidth, justifiedLine.WidthIncludingTrailingWhitespace, 3);

                // The last visible glyph is unchanged; the first glyph is widened.
                Assert.Equal(before[before.Count - 1], after[after.Count - 1], 3);
                AssertGreaterThan(after[0], before[0], "The first glyph should be widened");
            }
        }

        [Fact]
        public void Does_Not_Justify_Last_Line_Of_Wrapped_Paragraph()
        {
            using (Start())
            {
                // The last line of a justified paragraph stays start-aligned (its natural width),
                // while the preceding wrapped lines fill to the margin.
                var text = new string('一', 40);
                const double maxWidth = 80;

                var foreground = Brushes.Black.ToImmutable();

                var left = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Left, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                var justified = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Justify, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                var lineCount = justified.TextLines.Count;

                Assert.True(lineCount >= 2);

                var lastLeft = left.TextLines[lineCount - 1];
                var lastJustified = justified.TextLines[lineCount - 1];

                // Precondition: the last line is shorter than the margin, so stretching would show.
                AssertGreaterThan(maxWidth, lastLeft.WidthIncludingTrailingWhitespace,
                    "The last line must be shorter than MaxWidth for the test to be meaningful");

                // The last line is not stretched.
                Assert.Equal(lastLeft.WidthIncludingTrailingWhitespace,
                    lastJustified.WidthIncludingTrailingWhitespace, 3);

                // A preceding wrapped line is still justified to the margin.
                Assert.Equal(maxWidth, justified.TextLines[0].WidthIncludingTrailingWhitespace, 3);
            }
        }

        [Fact]
        public void Does_Not_Justify_Line_Ending_In_Hard_Break()
        {
            using (Start())
            {
                // "一二三" ends in an explicit newline (a hard break); it stays start-aligned while
                // the following width-wrapped, non-last line fills to the margin.
                var text = "一二三\n" + new string('一', 40);
                const double maxWidth = 80;

                var foreground = Brushes.Black.ToImmutable();

                var left = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Left, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                var justified = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Justify, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                // Line 0 ends in '\n', line 1 is a wrapped non-last line, line 2 is the last line.
                Assert.True(justified.TextLines.Count >= 3);

                var hardBreakLeft = left.TextLines[0];
                var hardBreakJustified = justified.TextLines[0];

                AssertGreaterThan(maxWidth, hardBreakLeft.WidthIncludingTrailingWhitespace,
                    "The hard-break line must be shorter than MaxWidth for the test to be meaningful");

                // The hard-break line is not stretched.
                Assert.Equal(hardBreakLeft.WidthIncludingTrailingWhitespace,
                    hardBreakJustified.WidthIncludingTrailingWhitespace, 3);

                // The following width-wrapped, non-last line is justified to the margin.
                Assert.Equal(maxWidth, justified.TextLines[1].WidthIncludingTrailingWhitespace, 3);
            }
        }

        [Fact]
        public void Justify_Does_Not_Mutate_Shared_ShapedBuffer()
        {
            using (Start())
            {
                // A TextRunCache keeps the same ShapedTextRun (and its pooled glyph storage) alive
                // across layouts. Justification must copy-on-write rather than mutate that shared
                // buffer in place. Simulate the cache's reference with AddRef and assert the
                // original buffer is untouched while the line's run is replaced with a widened copy.
                const string text = "一二三四五";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var originalRun = textLine.TextRuns.OfType<ShapedTextRun>().First();
                var originalBuffer = originalRun.ShapedBuffer;
                var originalFirstAdvance = originalBuffer[0].GlyphAdvance;

                // Second owner (stands in for the TextRunCache) so the buffer survives the run's
                // disposal during justification.
                originalRun.AddRef();

                textLine.Justify(new InterWordJustification(originalRun.Size.Width + 40));

                // The shared buffer is not mutated...
                Assert.Equal(originalFirstAdvance, originalBuffer[0].GlyphAdvance, 5);

                // ...and the line now holds a different run whose first glyph was widened.
                var justifiedRun = textLine.TextRuns.OfType<ShapedTextRun>().First();

                Assert.NotSame(originalRun, justifiedRun);
                AssertGreaterThan(justifiedRun.ShapedBuffer[0].GlyphAdvance, originalFirstAdvance,
                    "The justified copy's first glyph should be widened");

                originalRun.Dispose();
            }
        }

        [Fact]
        public void Justify_Repoints_Indexed_Runs_At_Replacement()
        {
            using (Start())
            {
                // Draw and hit-test resolve runs through the bidi-reordered IndexedTextRun list, so
                // after justification replaces a run its IndexedTextRun must point at the
                // replacement. GetTextBounds walks the indexed runs; its total must match the
                // justified line width, not the pre-justification width.
                var text = new string('一', 40);
                const double maxWidth = 80;

                var justified = new TextLayout(text, Typeface.Default, 12.0f, Brushes.Black.ToImmutable(),
                    textAlignment: TextAlignment.Justify, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                Assert.True(justified.TextLines.Count >= 2);

                var line = justified.TextLines[0];

                var bounds = line.GetTextBounds(line.FirstTextSourceIndex, line.Length);

                Assert.Equal(line.WidthIncludingTrailingWhitespace, bounds.Sum(b => b.Rectangle.Width), 2);
                Assert.Equal(maxWidth, bounds.Sum(b => b.Rectangle.Width), 2);
            }
        }

        [Fact]
        public void Justify_Distributes_Across_Multiple_Runs()
        {
            using (Start())
            {
                // Two shaped runs on one line (split by a font-size change). Each must receive its
                // own break opportunities: the pre-fix apply loop drained the whole queue against
                // the first run, leaving later runs unjustified.
                const string text = "一二三四五六";

                var first = new GenericTextRunProperties(Typeface.Default, 20);
                var second = new GenericTextRunProperties(Typeface.Default, 12);
                var textSource = new SplitStyleTextSource(text, 3, first, second);
                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(first));

                Assert.NotNull(textLine);

                var runs = textLine.TextRuns.OfType<ShapedTextRun>().ToList();

                // Confirm the line really is multi-run, otherwise the test proves nothing.
                Assert.True(runs.Count >= 2);

                var beforeWidths = runs.Select(r => r.Size.Width).ToList();

                textLine.Justify(new InterWordJustification(textLine.WidthIncludingTrailingWhitespace + 60));

                var afterRuns = textLine.TextRuns.OfType<ShapedTextRun>().ToList();

                Assert.Equal(runs.Count, afterRuns.Count);

                // Every shaped run participated in justification, not just the first.
                for (var i = 0; i < afterRuns.Count; i++)
                {
                    AssertGreaterThan(afterRuns[i].Size.Width, beforeWidths[i], $"Run {i} should be widened");
                }
            }
        }

        [Theory]
        [InlineData("aa bb   ")]     // trailing ASCII spaces
        [InlineData("一二　　　")]    // trailing ideographic (U+3000) spaces
        public void Justify_Does_Not_Space_Trailing_Whitespace(string text)
        {
            using (Start())
            {
                // Trailing whitespace (which sits before a wrap point or hard break) must never
                // receive justification space; the full distributed amount lands in the visible
                // region instead. This is guaranteed by the LineBreakEnumerator (it emits no
                // non-required break inside trailing whitespace), so no explicit guard is needed in
                // InterWordJustification - this test locks that invariant. Width excludes trailing
                // whitespace, so it must grow by the entire distributed space; if trailing
                // whitespace absorbed part of it, Width would grow less.
                const double extra = 40;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                // Confirm the line actually carries trailing whitespace, otherwise the test proves nothing.
                Assert.True(textLine.TrailingWhitespaceLength >= 2);
                AssertGreaterThan(textLine.WidthIncludingTrailingWhitespace, textLine.Width,
                    "The line must have measurable trailing whitespace");

                var widthBefore = textLine.Width;

                textLine.Justify(new InterWordJustification(textLine.Width + extra));

                Assert.Equal(widthBefore + extra, textLine.Width, 2);
            }
        }

        [Fact]
        public void Should_Justify_Wrapped_Latin_Line()
        {
            // The classic inter-word case. A wrapped non-last Latin line fills to the margin; the
            // last line stays start-aligned.
            using (Start())
            {
                AssertWrappedNonLastLineFillsToMaxWidth(
                    "the quick brown fox jumps over the lazy dog and then runs away quite quickly today", 140);
            }
        }

        // Korean Hangul justifies inter-syllable via the same code path as CJK (LB26/27 bind within
        // a syllable, LB31 breaks between syllable blocks). A live Korean advance test is not
        // possible here because the test harness's fallback fonts render Hangul with zero advance
        // (no Korean font installed), so the CJK tests stand in for it.

        [Fact]
        public void Does_Not_Justify_Latin_Line_With_Trailing_Spaces_Before_Hard_Break()
        {
            using (Start())
            {
                // A line ending in trailing spaces + a hard break stays start-aligned (its trailing
                // whitespace is not stretched), while the following wrapped lines still fill to the
                // margin.
                var text = "aa bb   \n" + string.Join(" ", Enumerable.Repeat("cc", 40));
                const double maxWidth = 120;

                var foreground = Brushes.Black.ToImmutable();

                var left = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Left, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                var justified = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                    textAlignment: TextAlignment.Justify, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

                Assert.True(justified.TextLines.Count >= 3);

                var line0Left = left.TextLines[0];
                var line0Justified = justified.TextLines[0];

                AssertGreaterThan(maxWidth, line0Left.WidthIncludingTrailingWhitespace,
                    "The hard-break line must be shorter than MaxWidth for the test to be meaningful");

                // The trailing-spaces + '\n' line is not stretched.
                Assert.Equal(line0Left.WidthIncludingTrailingWhitespace,
                    line0Justified.WidthIncludingTrailingWhitespace, 2);

                // A following width-wrapped, non-last line fills its visible content to the margin.
                Assert.Equal(maxWidth, justified.TextLines[1].Width, 2);
            }
        }

        [Fact]
        public void Justify_Distributes_Across_Latin_And_CJK()
        {
            using (Start())
            {
                // A mixed Latin+CJK line distributes space across both the inter-word gap and the
                // inter-ideograph gaps (the line reaches the target width) without stretching the
                // last visible glyph.
                const string text = "ab 日本語";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var lastGlyphBefore = LastGlyphAdvance(textLine);

                var target = textLine.WidthIncludingTrailingWhitespace + 40;

                textLine.Justify(new InterWordJustification(target));

                // Space was distributed across the mixed content (the line reaches the target)...
                Assert.Equal(target, textLine.WidthIncludingTrailingWhitespace, 2);

                // ...but the last visible glyph is not stretched.
                Assert.Equal(lastGlyphBefore, LastGlyphAdvance(textLine), 3);
            }
        }

        [Fact]
        public void Justify_Arabic_Does_Not_Corrupt_Shared_Buffer()
        {
            using (Start())
            {
                // Arabic is cursive and right-to-left (ligated, not one-char-per-cluster), so
                // justification exercises the copy-on-write path on a non-trivial run. It must run
                // without corrupting a cache-shared buffer.
                const string text = "مرحبا بالعالم";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var originalRun = textLine.TextRuns.OfType<ShapedTextRun>().First();
                var originalBuffer = originalRun.ShapedBuffer;
                var originalAdvances = Enumerable.Range(0, originalBuffer.Length)
                    .Select(i => originalBuffer[i].GlyphAdvance).ToList();

                // Second owner (stands in for a TextRunCache) so the buffer survives run disposal.
                originalRun.AddRef();

                var widthBefore = textLine.WidthIncludingTrailingWhitespace;

                textLine.Justify(new InterWordJustification(widthBefore + 40));

                // Justification happened (the inter-word gap was widened)...
                AssertGreaterThan(textLine.WidthIncludingTrailingWhitespace, widthBefore,
                    "Justifying an Arabic line should widen it");

                // ...and the shared buffer was not mutated in place.
                for (var i = 0; i < originalBuffer.Length; i++)
                {
                    Assert.Equal(originalAdvances[i], originalBuffer[i].GlyphAdvance, 5);
                }

                originalRun.Dispose();
            }
        }

        private static void AssertWrappedNonLastLineFillsToMaxWidth(string text, double maxWidth)
        {
            var foreground = Brushes.Black.ToImmutable();

            var left = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                textAlignment: TextAlignment.Left, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

            var justified = new TextLayout(text, Typeface.Default, 12.0f, foreground,
                textAlignment: TextAlignment.Justify, textWrapping: TextWrapping.Wrap, maxWidth: maxWidth);

            Assert.True(justified.TextLines.Count >= 2);

            var leftLine = left.TextLines[0];
            var justifiedLine = justified.TextLines[0];

            AssertGreaterThan(maxWidth, leftLine.Width,
                "The unjustified line's visible content must be narrower than MaxWidth");

            // The non-last wrapped line's visible content fills to the margin. (Width excludes any
            // trailing whitespace, which hangs past the margin.)
            Assert.Equal(maxWidth, justifiedLine.Width, 2);

            // The last line stays start-aligned.
            var lastLeft = left.TextLines[left.TextLines.Count - 1];
            var lastJustified = justified.TextLines[justified.TextLines.Count - 1];

            Assert.Equal(lastLeft.WidthIncludingTrailingWhitespace,
                lastJustified.WidthIncludingTrailingWhitespace, 2);
        }

        private static double LastGlyphAdvance(TextLine line)
        {
            var glyphs = line.TextRuns.OfType<ShapedTextRun>().Last().GlyphRun.GlyphInfos;

            return glyphs[glyphs.Count - 1].GlyphAdvance;
        }

        private sealed class SplitStyleTextSource : ITextSource
        {
            private readonly string _text;
            private readonly int _splitAt;
            private readonly GenericTextRunProperties _first;
            private readonly GenericTextRunProperties _second;

            public SplitStyleTextSource(string text, int splitAt,
                GenericTextRunProperties first, GenericTextRunProperties second)
            {
                _text = text;
                _splitAt = splitAt;
                _first = first;
                _second = second;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _text.Length)
                {
                    return null;
                }

                if (textSourceIndex < _splitAt)
                {
                    return new TextCharacters(_text.AsMemory(textSourceIndex, _splitAt - textSourceIndex), _first);
                }

                return new TextCharacters(_text.AsMemory(textSourceIndex), _second);
            }
        }

        private static List<double> GlyphAdvances(TextLine line)
        {
            var advances = new List<double>();

            foreach (var run in line.TextRuns)
            {
                if (run is ShapedTextRun shaped)
                {
                    foreach (var glyph in shaped.GlyphRun.GlyphInfos)
                    {
                        advances.Add(glyph.GlyphAdvance);
                    }
                }
            }

            return advances;
        }

        [Fact]
        public void Width_Excludes_Only_The_Lines_True_Trailing_Whitespace()
        {
            using (Start())
            {
                // Two runs split by a font-size change. The FIRST (interior) run also ends in a
                // space of its own ("foo "), but that space is followed by more visible content
                // ("bar") in the next run, so it is NOT trailing whitespace for the line as a
                // whole - only the space at the true end of the line is. A reference line without
                // any trailing space isolates exactly how much Width should differ.
                var first = new GenericTextRunProperties(Typeface.Default, 20);
                var second = new GenericTextRunProperties(Typeface.Default, 14);
                var formatter = new TextFormatterImpl();

                var reference = formatter.FormatLine(new SplitStyleTextSource("foo bar", 4, first, second), 0,
                    double.PositiveInfinity, new GenericTextParagraphProperties(first));

                var withTrailingSpace = formatter.FormatLine(new SplitStyleTextSource("foo bar ", 4, first, second), 0,
                    double.PositiveInfinity, new GenericTextParagraphProperties(first));

                Assert.NotNull(reference);
                Assert.NotNull(withTrailingSpace);

                // Confirm both lines are genuinely multi-run, and the reference truly has no
                // trailing whitespace, otherwise the comparison proves nothing.
                Assert.True(reference.TextRuns.OfType<ShapedTextRun>().Count() >= 2);
                Assert.Equal(0, reference.TrailingWhitespaceLength);

                // Only the one added trailing space is excluded - not that space AND "foo "'s own
                // interior trailing space too.
                Assert.Equal(1, withTrailingSpace.TrailingWhitespaceLength);
                Assert.Equal(reference.Width, withTrailingSpace.Width, 3);
            }
        }

        private static void AssertGreaterThan(double x, double y, string message) => Assert.True(x > y, $"{message}. {x} is not > {y}");

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
