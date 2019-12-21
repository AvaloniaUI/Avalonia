using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.UnitTests;
using Avalonia.Utility;
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
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(1, 2, foreground: foreground)
                };

                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity),
                    spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(3, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.GlyphRun.Characters.Length);

                var actual = textRun.GlyphRun.Characters.AsSpan().ToString();

                Assert.Equal("12", actual);

                Assert.Equal(foreground, textRun.Foreground);
            }
        }

        [Fact]
        public void Should_Not_Alter_Lines_After_TextStyleSpan_Was_Applied()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                for (var i = 1; i < s_multiLineText.Length; i++)
                {
                    var spans = new List<FormattedTextStyleSpan>
                    {
                        new FormattedTextStyleSpan(0, i, foreground: foreground)
                    };

                    var expected = new TextLayout(
                        s_multiLineText,
                        Typeface.Default,
                        12.0f,
                        TextAlignment.Left,
                        TextWrapping.Wrap,
                        TextTrimming.None,
                        new Size(25, double.PositiveInfinity));

                    var actual = new TextLayout(
                        s_multiLineText,
                        Typeface.Default,
                        12.0f,
                        TextAlignment.Left,
                        TextWrapping.Wrap,
                        TextTrimming.None,
                        new Size(25, double.PositiveInfinity),
                        spans);

                    Assert.Equal(expected.TextLines.Count, actual.TextLines.Count);

                    for (var j = 0; j < actual.TextLines.Count; j++)
                    {
                        Assert.Equal(expected.TextLines[j].Text.Length, actual.TextLines[j].Text.Length);

                        Assert.Equal(expected.TextLines[j].TextRuns.Sum(x => x.GlyphRun.Characters.Length),
                            actual.TextLines[j].TextRuns.Sum(x => x.GlyphRun.Characters.Length));
                    }
                }
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Text_At_Start()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(0, 2, foreground: foreground)
                };

                var layout = new TextLayout(
                    s_singleLineText,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity),
                    spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(2, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(2, textRun.GlyphRun.Characters.Length);

                var actual = s_singleLineText.Substring(textRun.GlyphRun.Characters.Start,
                    textRun.GlyphRun.Characters.Length);

                Assert.Equal("01", actual);

                Assert.Equal(foreground, textRun.Foreground);
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Text_At_End()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(8, 2, foreground: foreground)
                };

                var layout = new TextLayout(
                    s_singleLineText,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity),
                    spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(2, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.GlyphRun.Characters.Length);

                var actual = textRun.GlyphRun.Characters.AsSpan().ToString();

                Assert.Equal("89", actual);

                Assert.Equal(foreground, textRun.Foreground);
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_Single_Character()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(0, 1, foreground: foreground)
                };

                var layout = new TextLayout(
                    "0",
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity),
                    spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(1, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(1, textRun.GlyphRun.Characters.Length);

                Assert.Equal(foreground, textRun.Foreground);
            }
        }

        [Fact]
        public void Should_Apply_TextSpan_To_Unicode_String_In_Between()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
                renderInterface: new PlatformRenderInterface(null), textFormatter: new TextFormatter())))
            {
                const string text = "😄😄😄😄";

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(2, 2, foreground: foreground)
                };

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity),
                    spans);

                var textLine = layout.TextLines[0];

                Assert.Equal(3, textLine.TextRuns.Count);

                var textRun = textLine.TextRuns[1];

                Assert.Equal(2, textRun.GlyphRun.Characters.Length);

                var actual = textRun.GlyphRun.Characters.AsSpan().ToString();

                Assert.Equal("😄", actual);

                Assert.Equal(foreground, textRun.Foreground);
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextLine_Length_Sum()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity));

                Assert.Equal(s_multiLineText.Length, layout.TextLines.Sum(x => x.Text.Length));
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextRun_TextLength_Sum()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity));

                Assert.Equal(
                    s_multiLineText.Length,
                    layout.TextLines.Select(textLine =>
                            textLine.TextRuns.Sum(textRun => textRun.GlyphRun.Characters.Length))
                        .Sum());
            }
        }

        [Fact]
        public void TextLength_Should_Be_Equal_To_TextRun_TextLength_Sum_After_Wrap_With_Style_Applied()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                const string text =
                    "Multiline TextBox with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. " +
                    "Curabitur massa. Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus pretium ornare est.";

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(0, 24, foreground: foreground)
                };

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.Wrap,
                    TextTrimming.None,
                    new Size(180, double.PositiveInfinity),
                    spans);

                Assert.Equal(
                    text.Length,
                    layout.TextLines.Select(textLine =>
                            textLine.TextRuns.Sum(textRun => textRun.GlyphRun.Characters.Length))
                        .Sum());
            }
        }

        [Fact]
        public void Should_Apply_TextStyleSpan_To_MultiLine()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var spans = new List<FormattedTextStyleSpan>
                {
                    new FormattedTextStyleSpan(5, 20, foreground: foreground)
                };

                var layout = new TextLayout(
                    s_multiLineText,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(200, 125),
                    spans);

                Assert.Equal(foreground, layout.TextLines[0].TextRuns[1].Foreground);
                Assert.Equal(foreground, layout.TextLines[1].TextRuns[0].Foreground);
                Assert.Equal(foreground, layout.TextLines[2].TextRuns[0].Foreground);
            }
        }

        [Fact( /*Skip = "Currently fails on Linux because of not present Emojis font"*/)]
        public void Should_Hit_Test_SurrogatePair()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                const string text = "😄😄";

                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity));

                var lineMetrics = layout.TextLines[0].LineMetrics;

                var width = lineMetrics.Size.Width;

                var hitTestResult = layout.HitTestPoint(new Point(width, lineMetrics.BaselineOrigin.Y));

                Assert.Equal(2, hitTestResult.CharacterHit.FirstCharacterIndex);

                Assert.Equal(2, hitTestResult.CharacterHit.TrailingLength);
            }
        }


        [Theory]
        [InlineData("☝🏿", new ushort[] { 0 })]
        [InlineData("☝🏿 ab", new ushort[] { 0, 3, 4, 5 })]
        [InlineData("ab ☝🏿", new ushort[] { 0, 1, 2, 3 })]
        public void Should_Create_Valid_Clusters_For_Text(string text, ushort[] clusters)
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity));

                var textLine = layout.TextLines[0];

                var index = 0;

                foreach (var textRun in textLine.TextRuns)
                {
                    var glyphRun = textRun.GlyphRun;

                    var glyphClusters = glyphRun.GlyphClusters;

                    var expected = clusters.Skip(index).Take(glyphClusters.Length);

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
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var layout = new TextLayout(
                    text,
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity));

                Assert.Equal(2, layout.TextLines.Count);

                Assert.Equal(1, layout.TextLines[0].TextRuns.Count);

                Assert.Equal(7, layout.TextLines[0].TextRuns[0].GlyphRun.GlyphClusters.Length);

                Assert.Equal(5, layout.TextLines[0].TextRuns[0].GlyphRun.GlyphClusters[5]);

                Assert.Equal(5, layout.TextLines[0].TextRuns[0].GlyphRun.GlyphClusters[6]);
            }
        }

        [Fact]
        public void Should_Have_One_Run_With_Common_Script()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var layout = new TextLayout(
                    "abcde\r\n",
                    Typeface.Default,
                    12.0f,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    new Size(double.PositiveInfinity, double.PositiveInfinity));

                Assert.Equal(1, layout.TextLines[0].TextRuns.Count);
            }
        }

        [Theory]
        [InlineData("0123", 1)]
        [InlineData("\r\n", 1)]
        [InlineData("👍b", 2 /*, Skip = "Currently fails on Linux because of not present fonts"*/)]
        [InlineData("a👍b", 3 /*, Skip = "Currently fails on Linux because of not present fonts"*/)]
        [InlineData("a👍子b", 4 /*, Skip = "Currently fails on Linux because of not present fonts"*/)]
        public void Should_Produce_Unique_Runs(string text, int numberOfRuns)
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(
                    renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var buffer = new ReadOnlySlice<char>(text.AsMemory());

                var breaker = TextRunIterator.Create(buffer, Typeface.Default, 12);

                var runs = breaker.ToArray();

                Assert.Equal(numberOfRuns, runs.Length);
            }
        }

        [Fact]
        public void Should_Split_Run_On_Direction()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var text = "1234الدولي";

                var buffer = new ReadOnlySlice<char>(text.AsMemory());

                var breaker = TextRunIterator.Create(buffer, Typeface.Default, 12);

                Assert.Equal(2, breaker.Count);

                Assert.Equal(4, breaker[0].Text.Length);
            }
        }

        [Fact]
        public void Should_Layout_Corrupted_Text()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatter: new TextFormatter())))
            {
                var text = new string(new[] { '\uD802', '\uD802', '\uD802', '\uD802', '\uD802', '\uD802', '\uD802' });

                var typeface = Typeface.Default;

                var layout = new TextLayout(text, typeface, 12, TextAlignment.Left, TextWrapping.NoWrap,
                    TextTrimming.None, Size.Infinity);

                var textLine = layout.TextLines[0];

                var textRun = textLine.TextRuns[0];

                Assert.Equal(7, textRun.GlyphRun.Characters.Length);

                var replacementGlyph = typeface.GlyphTypeface.GetGlyph('\uFFFD');

                foreach (var glyph in textRun.GlyphRun.GlyphIndices)
                {
                    Assert.Equal(replacementGlyph, glyph);
                }
            }
        }
    }
}
