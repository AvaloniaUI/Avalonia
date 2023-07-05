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
        [InlineData("☝🏿 ab", new int[] { 0, 3, 4, 5 })]
        [InlineData("ab ☝🏿", new int[] { 0, 1, 2, 3 })]
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

                var replacementGlyph = Typeface.Default.GlyphTypeface.GetGlyph(Codepoint.ReplacementCodepoint);

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

                var firstRun = layout.TextLines[0].TextRuns[0] as ShapedTextRun;

                var hit = layout.HitTestPoint(new Point());

                Assert.Equal(4, hit.TextPosition);

                var currentX = 0.0;

                for (var i = 0; i < firstRun.GlyphRun.GlyphInfos.Count; i++)
                {
                    var cluster = firstRun.GlyphRun.GlyphInfos[i].GlyphCluster;
                    var advance = firstRun.GlyphRun.GlyphInfos[i].GlyphAdvance;

                    hit = layout.HitTestPoint(new Point(currentX, 0));

                    Assert.Equal(cluster, hit.TextPosition);

                    var hitRange = layout.HitTestTextRange(hit.TextPosition, 1);

                    var distance = hitRange.First().Left;

                    Assert.Equal(currentX, distance, 2);

                    currentX += advance;
                }

                var secondRun = layout.TextLines[0].TextRuns[1] as ShapedTextRun;

                hit = layout.HitTestPoint(new Point(firstRun.Size.Width, 0));

                Assert.Equal(7, hit.TextPosition);

                hit = layout.HitTestPoint(new Point(layout.TextLines[0].WidthIncludingTrailingWhitespace, 0));

                Assert.Equal(0, hit.TextPosition);

                currentX = firstRun.Size.Width + 0.5;

                for (var i = 0; i < secondRun.GlyphRun.GlyphInfos.Count; i++)
                {
                    var cluster = secondRun.GlyphRun.GlyphInfos[i].GlyphCluster;
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

                    var clusters = shapedRuns.SelectMany(x => x.ShapedBuffer, (_, glyph) => glyph.GlyphCluster).ToList();

                    var glyphAdvances = shapedRuns.SelectMany(x => x.ShapedBuffer, (_, glyph) => glyph.GlyphAdvance).ToList();

                    var currentX = 0.0;

                    var cluster = text.Length;

                    for (int j = 0; j < clusters.Count - 1; j++)
                    {                     
                        var glyphAdvance = glyphAdvances[j];

                        var characterHit = textLine.GetCharacterHitFromDistance(currentX);

                        Assert.Equal(cluster, characterHit.FirstCharacterIndex + characterHit.TrailingLength);

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

                Assert.Equal(14.0625, rect.Top);
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
