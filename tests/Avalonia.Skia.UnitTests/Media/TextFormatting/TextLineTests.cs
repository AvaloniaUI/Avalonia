#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextLineTests
    {
        private const string s_multiLineText = "012345678\r\r0123456789";

        [Fact]
        public void Should_Get_First_CharacterHit()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(s_multiLineText, defaultProperties);

                var formatter = new TextFormatterImpl();

                var currentIndex = 0;

                while (currentIndex < s_multiLineText.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentIndex, double.PositiveInfinity,
                            new GenericTextParagraphProperties(defaultProperties));

                    Assert.NotNull(textLine);

                    var firstCharacterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(int.MinValue));

                    Assert.Equal(textLine.FirstTextSourceIndex, firstCharacterHit.FirstCharacterIndex);

                    currentIndex += textLine.Length;
                }
            }
        }

        [Fact]
        public void Should_Get_Last_CharacterHit()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(s_multiLineText, defaultProperties);

                var formatter = new TextFormatterImpl();

                var currentIndex = 0;

                while (currentIndex < s_multiLineText.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentIndex, double.PositiveInfinity,
                            new GenericTextParagraphProperties(defaultProperties));

                    Assert.NotNull(textLine);

                    var lastCharacterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(int.MaxValue));

                    Assert.Equal(textLine.FirstTextSourceIndex + textLine.Length,
                        lastCharacterHit.FirstCharacterIndex + lastCharacterHit.TrailingLength);

                    currentIndex += textLine.Length;
                }
            }
        }

        [Fact]
        public void Should_Get_Next_Caret_CharacterHit_Bidi()
        {
            const string text = "אבג 1 ABC";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var clusters = new List<int>();

                foreach (var textRun in textLine.TextRuns.OrderBy(x => TextTestHelper.GetStartCharIndex(x.Text)))
                {
                    var shapedRun = (ShapedTextRun)textRun;

                    var runClusters = shapedRun.ShapedBuffer.Select(glyph => glyph.GlyphCluster);

                    clusters.AddRange(shapedRun.IsReversed ? runClusters.Reverse() : runClusters);
                }

                var nextCharacterHit = new CharacterHit(0, clusters[1] - clusters[0]);

                foreach (var cluster in clusters)
                {
                    Assert.Equal(cluster, nextCharacterHit.FirstCharacterIndex);

                    nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);
                }

                var lastCharacterHit = nextCharacterHit;

                nextCharacterHit = textLine.GetNextCaretCharacterHit(lastCharacterHit);

                Assert.Equal(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

                Assert.Equal(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Get_Previous_Caret_CharacterHit_Bidi()
        {
            const string text = "אבג 1 ABC";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var clusters = new List<int>();

                foreach (var textRun in textLine.TextRuns.OrderBy(x => TextTestHelper.GetStartCharIndex(x.Text)))
                {
                    var shapedRun = (ShapedTextRun)textRun;

                    var runClusters = shapedRun.ShapedBuffer.Select(glyph => glyph.GlyphCluster);

                    clusters.AddRange(shapedRun.IsReversed ? runClusters.Reverse() : runClusters);
                }

                clusters.Reverse();

                var nextCharacterHit = new CharacterHit(text.Length - 1);

                foreach (var cluster in clusters)
                {
                    var currentCaretIndex = nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength;

                    Assert.Equal(cluster, currentCaretIndex);

                    nextCharacterHit = textLine.GetPreviousCaretCharacterHit(nextCharacterHit);
                }

                var lastCharacterHit = nextCharacterHit;

                nextCharacterHit = textLine.GetPreviousCaretCharacterHit(lastCharacterHit);

                Assert.Equal(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

                Assert.Equal(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);
            }
        }

        [InlineData("𐐷𐐷𐐷𐐷𐐷")]
        [InlineData("01234567🎉\n")]
        [InlineData("𐐷1234")]
        [Theory]
        public void Should_Get_Next_Caret_CharacterHit(string text)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var clusters = BuildGlyphClusters(textLine);

                var nextCharacterHit = new CharacterHit(0);

                for (var i = 0; i < clusters.Count; i++)
                {
                    var expectedCluster = clusters[i];
                    var actualCluster = nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength;

                    Assert.Equal(expectedCluster, actualCluster);

                    nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);
                }

                var lastCharacterHit = nextCharacterHit;

                nextCharacterHit = textLine.GetNextCaretCharacterHit(lastCharacterHit);

                Assert.Equal(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

                Assert.Equal(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);

                nextCharacterHit = new CharacterHit(0, clusters[1] - clusters[0]);

                foreach (var cluster in clusters)
                {
                    Assert.Equal(cluster, nextCharacterHit.FirstCharacterIndex);

                    nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);
                }

                lastCharacterHit = nextCharacterHit;

                nextCharacterHit = textLine.GetNextCaretCharacterHit(lastCharacterHit);

                Assert.Equal(lastCharacterHit.FirstCharacterIndex, nextCharacterHit.FirstCharacterIndex);

                Assert.Equal(lastCharacterHit.TrailingLength, nextCharacterHit.TrailingLength);
            }
        }

        [InlineData("𐐷𐐷𐐷𐐷𐐷")]
        [InlineData("01234567🎉\n")]
        [InlineData("𐐷1234")]
        [Theory]
        public void Should_Get_Previous_Caret_CharacterHit(string text)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var clusters = textLine.TextRuns
                    .Cast<ShapedTextRun>()
                    .SelectMany(x => x.ShapedBuffer, (_, glyph) => glyph.GlyphCluster)
                    .ToArray();

                var previousCharacterHit = new CharacterHit(text.Length);

                for (var i = clusters.Length - 1; i >= 0; i--)
                {
                    previousCharacterHit = textLine.GetPreviousCaretCharacterHit(previousCharacterHit);

                    Assert.Equal(clusters[i],
                        previousCharacterHit.FirstCharacterIndex + previousCharacterHit.TrailingLength);
                }

                var firstCharacterHit = previousCharacterHit;

                previousCharacterHit = textLine.GetPreviousCaretCharacterHit(firstCharacterHit);

                Assert.Equal(firstCharacterHit.FirstCharacterIndex, previousCharacterHit.FirstCharacterIndex);

                Assert.Equal(0, previousCharacterHit.TrailingLength);

                previousCharacterHit = new CharacterHit(clusters[^1], text.Length - clusters[^1]);

                for (var i = clusters.Length - 1; i > 0; i--)
                {
                    previousCharacterHit = textLine.GetPreviousCaretCharacterHit(previousCharacterHit);

                    Assert.Equal(clusters[i],
                        previousCharacterHit.FirstCharacterIndex + previousCharacterHit.TrailingLength);
                }
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(s_multiLineText, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var currentDistance = 0.0;

                foreach (var run in textLine.TextRuns)
                {
                    var textRun = (ShapedTextRun)run;

                    var glyphRun = textRun.GlyphRun;

                    for (var i = 0; i < glyphRun.GlyphInfos.Count; i++)
                    {
                        var cluster = glyphRun.GlyphInfos[i].GlyphCluster;

                        var advance = glyphRun.GlyphInfos[i].GlyphAdvance;

                        var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

                        Assert.Equal(currentDistance, distance);

                        currentDistance += advance;
                    }
                }

                var actualDistance = textLine.GetDistanceFromCharacterHit(new CharacterHit(s_multiLineText.Length));

                Assert.Equal(currentDistance, actualDistance);
            }
        }

        [InlineData("ABC012345")] //LeftToRight
        [InlineData("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן")] //RightToLeft
        [Theory]
        public void Should_Get_CharacterHit_From_Distance(string text)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var isRightToLeft = IsRightToLeft(textLine);
                var rects = BuildRects(textLine);
                var glyphClusters = BuildGlyphClusters(textLine);

                for (var i = 0; i < rects.Count; i++)
                {
                    var cluster = glyphClusters[i];
                    var rect = rects[i];

                    var characterHit = textLine.GetCharacterHitFromDistance(rect.Left);

                    Assert.Equal(isRightToLeft ? cluster + 1 : cluster,
                        characterHit.FirstCharacterIndex + characterHit.TrailingLength);
                }
            }
        }

        public static IEnumerable<object[]> CollapsingData
        {
            get
            {
                yield return CreateData("01234 01234 01234", 120, TextTrimming.PrefixCharacterEllipsis, "01234 01\u20264 01234");
                yield return CreateData("01234 01234", 58, TextTrimming.CharacterEllipsis, "01234 0\u2026");
                yield return CreateData("01234 01234", 58, TextTrimming.WordEllipsis, "01234\u2026");
                yield return CreateData("01234", 9, TextTrimming.CharacterEllipsis, "\u2026");
                yield return CreateData("01234", 2, TextTrimming.CharacterEllipsis, "");

                object[] CreateData(string text, double width, TextTrimming mode, string expected)
                {
                    return new object[]
                    {
                        text, width, mode, expected
                    };
                }
            }
        }

        [MemberData(nameof(CollapsingData))]
        [Theory]
        public void Should_Collapse_Line(string text, double width, TextTrimming trimming, string expected)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.False(textLine.HasCollapsed);

                TextCollapsingProperties collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(width, defaultProperties, FlowDirection.LeftToRight));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.True(collapsedLine.HasCollapsed);

                var trimmedText = collapsedLine.TextRuns.SelectMany(x => x.Text.ToString()).ToArray();

                Assert.Equal(expected.Length, trimmedText.Length);

                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.Equal(expected[i], trimmedText[i]);
                }
            }
        }

        [Fact]
        public void Should_Get_Next_CharacterHit_For_Drawable_Runs()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new DrawableRunTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(4, textLine.TextRuns.Count);

                var currentHit = textLine.GetNextCaretCharacterHit(new CharacterHit(0));

                Assert.Equal(1, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);

                currentHit = textLine.GetNextCaretCharacterHit(currentHit);

                Assert.Equal(2, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);

                currentHit = textLine.GetNextCaretCharacterHit(currentHit);

                Assert.Equal(3, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);

                currentHit = textLine.GetNextCaretCharacterHit(currentHit);

                Assert.Equal(4, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Get_Previous_CharacterHit_For_Drawable_Runs()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new DrawableRunTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(4, textLine.TextRuns.Count);

                var currentHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(3, 1));

                Assert.Equal(3, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);

                currentHit = textLine.GetPreviousCaretCharacterHit(currentHit);

                Assert.Equal(2, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);

                currentHit = textLine.GetPreviousCaretCharacterHit(currentHit);

                Assert.Equal(1, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);

                currentHit = textLine.GetPreviousCaretCharacterHit(currentHit);

                Assert.Equal(0, currentHit.FirstCharacterIndex);
                Assert.Equal(0, currentHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Get_CharacterHit_From_Distance_For_Drawable_Runs()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new DrawableRunTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var characterHit = textLine.GetCharacterHitFromDistance(50);

                Assert.Equal(5, characterHit.FirstCharacterIndex);
                Assert.Equal(1, characterHit.TrailingLength);

                characterHit = textLine.GetCharacterHitFromDistance(32);

                Assert.Equal(3, characterHit.FirstCharacterIndex);
                Assert.Equal(0, characterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit_Drawable_Runs()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new DrawableRunTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(1));

                Assert.Equal(14, distance);

                distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(2));

                Assert.True(distance > 14);
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit_Mixed_TextBuffer()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new MixedTextBufferTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(10));

                Assert.Equal(72.01171875, distance);

                distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(20));

                Assert.Equal(144.0234375, distance);

                distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(30));

                Assert.Equal(216.03515625, distance);

                distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(40));

                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, distance);
            }
        }

        [Fact]
        public void Should_Get_TextBounds_From_Mixed_TextBuffer()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new MixedTextBufferTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, 10);

                Assert.Equal(1, textBounds.Count);

                Assert.Equal(72.01171875, textBounds[0].Rectangle.Width);

                textBounds = textLine.GetTextBounds(0, 20);

                Assert.Equal(1, textBounds.Count);

                Assert.Equal(144.0234375, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(0, 30);

                Assert.Equal(1, textBounds.Count);

                Assert.Equal(216.03515625, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(0, 40);

                Assert.Equal(1, textBounds.Count);

                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width));
            }
        }

        [Fact]
        public void Should_Get_TextBounds_For_LineBreak()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(Environment.NewLine, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, Environment.NewLine.Length);

                Assert.Equal(1, textBounds.Count);

                Assert.Equal(1, textBounds[0].TextRunBounds.Count);

                Assert.Equal(Environment.NewLine.Length, textBounds[0].TextRunBounds[0].Length);
            }
        }

        [Fact]
        public void Should_GetTextRange()
        {
            var text = "שדגככעיחדגכAישדגשדגחייטYDASYWIWחיחלדשSAטויליHUHIUHUIDWKLאא'ק'קחליק/'וקןגגגלךשף'/קפוכדגכשדגשיח'/קטאגשד";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

                var lineWidth = textLine.WidthIncludingTrailingWhitespace;

                var textBounds = textLine.GetTextBounds(0, text.Length);

                TextBounds? lastBounds = null;

                var runBounds = textBounds.SelectMany(x => x.TextRunBounds).ToList();

                Assert.Equal(textRuns.Count, runBounds.Count);

                for (var i = 0; i < textRuns.Count; i++)
                {
                    var run = textRuns[i];
                    var bounds = runBounds[i];

                    Assert.Equal(TextTestHelper.GetStartCharIndex(run.Text), bounds.TextSourceCharacterIndex);
                    Assert.Equal(run, bounds.TextRun);
                    Assert.Equal(run.Size.Width, bounds.Rectangle.Width, 2);
                }

                for (var i = 0; i < textBounds.Count; i++)
                {
                    var currentBounds = textBounds[i];

                    if (lastBounds != null)
                    {
                        Assert.Equal(lastBounds.Rectangle.Right, currentBounds.Rectangle.Left, 2);
                    }

                    var sumOfRunWidth = currentBounds.TextRunBounds.Sum(x => x.Rectangle.Width);

                    Assert.Equal(sumOfRunWidth, currentBounds.Rectangle.Width, 2);

                    lastBounds = currentBounds;
                }

                var sumOfBoundsWidth = textBounds.Sum(x => x.Rectangle.Width);

                Assert.Equal(lineWidth, sumOfBoundsWidth, 2);
            }
        }

        [Fact]
        public void Should_Get_CharacterHit_For_Distance_With_TextEndOfLine()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource("Hello World", defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 1000,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var characterHit = textLine.GetCharacterHitFromDistance(1000);

                Assert.Equal(10, characterHit.FirstCharacterIndex);
                Assert.Equal(1, characterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_GetNextCaretCharacterHit_From_Mixed_TextBuffer()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new MixedTextBufferTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(9, 1));

                Assert.Equal(10, characterHit.FirstCharacterIndex);

                Assert.Equal(1, characterHit.TrailingLength);

                characterHit = textLine.GetNextCaretCharacterHit(characterHit);

                Assert.Equal(11, characterHit.FirstCharacterIndex);

                Assert.Equal(1, characterHit.TrailingLength);

                characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(19, 1));

                Assert.Equal(20, characterHit.FirstCharacterIndex);

                Assert.Equal(1, characterHit.TrailingLength);

                characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(10));

                Assert.Equal(11, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetNextCaretCharacterHit(characterHit);

                Assert.Equal(12, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(20));

                Assert.Equal(21, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_GetPreviousCaretCharacterHit_From_Mixed_TextBuffer()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new MixedTextBufferTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(20, 1));

                Assert.Equal(20, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(10, 1));

                Assert.Equal(10, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetPreviousCaretCharacterHit(characterHit);

                Assert.Equal(9, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(21));

                Assert.Equal(20, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(11));

                Assert.Equal(10, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);

                characterHit = textLine.GetPreviousCaretCharacterHit(characterHit);

                Assert.Equal(9, characterHit.FirstCharacterIndex);

                Assert.Equal(0, characterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_GetCharacterHitFromDistance_From_Mixed_TextBuffer()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new MixedTextBufferTextSource();

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 20, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var characterHit = textLine.GetCharacterHitFromDistance(double.PositiveInfinity);

                Assert.Equal(40, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Throw_ArgumentOutOfRangeException_For_Zero_TextLength()
        {
            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(new TextCharacters("1234", defaultProperties));
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Throws<ArgumentOutOfRangeException>(() => textLine.GetTextBounds(0, 0));
            }
        }

        [Fact]
        public void Should_GetTextBounds_For_Negative_TextLength()
        {
            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(new TextCharacters("1234", defaultProperties));
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, -1);

                Assert.NotNull(textBounds);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.Empty(firstBounds.TextRunBounds);

                Assert.Equal(0, firstBounds.Rectangle.Width);

                Assert.Equal(0, firstBounds.Rectangle.Left);
            }
        }

        [Fact]
        public void Should_GetTextBounds_For_Exceeding_TextLength()
        {
            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(new TextCharacters("1234", defaultProperties));
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(10, 1);

                Assert.NotNull(textBounds);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.Empty(firstBounds.TextRunBounds);

                Assert.Equal(0, firstBounds.Rectangle.Width);

                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, firstBounds.Rectangle.Right);
            }
        }

        [Fact]
        public void Should_GetTextBounds_For_Mixed_Hidden_Runs_With_Ligature()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope"));

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(
                    new TextHidden(1),
                    new TextCharacters("Authenti", defaultProperties),
                    new TextHidden(1),
                    new TextHidden(1),
                    new TextCharacters("ff", defaultProperties),
                    new TextHidden(1),
                    new TextHidden(1));

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(12, 1);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.NotNull(firstBounds.TextRunBounds);
                Assert.NotEmpty(firstBounds.TextRunBounds);

                var firstRun = firstBounds.TextRunBounds[0];

                Assert.NotNull(firstRun);

                Assert.Equal(12, firstRun.TextSourceCharacterIndex);
            }
        }

        [Fact]
        public void Should_GetTextBounds_For_Mixed_Hidden_Runs()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope"));

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(
                    new TextHidden(1),
                    new TextCharacters("Authenti", defaultProperties),
                    new TextHidden(1),
                    new TextHidden(1),
                    new TextEndOfParagraph(1));

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(8, 1);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.NotNull(firstBounds.TextRunBounds);
                Assert.NotEmpty(firstBounds.TextRunBounds);

                var firstRun = firstBounds.TextRunBounds[0];

                Assert.NotNull(firstRun);

                Assert.Equal(8, firstRun.TextSourceCharacterIndex);
            }
        }

        [Win32Fact("Windows font")]
        public void Should_GetTextBounds_Within_Cluster()
        {
            using (Start())
            {
                var typeface = new Typeface("Segoe UI Emoji");

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(new TextCharacters("🙈", defaultProperties));
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, 1);

                Assert.NotEmpty(textBounds);

                var runBounds = textBounds[0].TextRunBounds[0];

                Assert.Equal(0, runBounds.TextSourceCharacterIndex);

                textBounds = textLine.GetTextBounds(1, 1);

                Assert.NotEmpty(textBounds);

                runBounds = textBounds[0].TextRunBounds[0];

                Assert.Equal(1, runBounds.TextSourceCharacterIndex);

                textBounds = textLine.GetTextBounds(2, 1);

                Assert.NotEmpty(textBounds);

                Assert.NotNull(textBounds[0].TextRunBounds);

                Assert.Empty(textBounds[0].TextRunBounds);
            }
        }

        [Win32Fact("Windows font")]
        public void Should_GetTextBounds_After_Last_Index()
        {
            using (Start())
            {
                var typeface = new Typeface("Segoe UI Emoji");

                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(new TextCharacters("🙈", defaultProperties));
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(2, 1);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.Equal(textLine.Width, firstBounds.Rectangle.Right);

                Assert.NotNull(firstBounds.TextRunBounds);

                Assert.Empty(firstBounds.TextRunBounds);
            }
        }

        [Fact]
        public void Should_Get_Run_Bounds()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope"));
                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new CustomTextBufferTextSource(
                    new TextCharacters("He", defaultProperties),
                    new TextCharacters("Wo", defaultProperties),
                    new TextCharacters("ff", defaultProperties));

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(1, 1);

                Assert.NotEmpty(textBounds);

                textBounds = textLine.GetTextBounds(2, 1);

                Assert.NotEmpty(textBounds);

                textBounds = textLine.GetTextBounds(4, 1);

                Assert.NotEmpty(textBounds);
            }
        }


        [Fact]
        public void Should_Handle_NewLine_In_RTL_Text()
        {
            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource("test\r\n", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Right,
                        true, true, defaultProperties, TextWrapping.Wrap, 0, 0, 0));

                Assert.NotNull(textLine);

                Assert.NotEqual(textLine.NewLineLength, 0);

            }
        }

        [Theory]
        [InlineData("hello\r\nworld")]
        [InlineData("مرحباً\r\nبالعالم")]
        [InlineData("hello مرحباً\r\nworld بالعالم")]
        [InlineData("مرحباً hello\r\nبالعالم nworld")]
        public void Should_Set_NewLineLength_For_CRLF_In_RTL_Text(string text)
        {
            using (Start())
            {
                var typeface = Typeface.Default;
                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
      formatter.FormatLine(textSource, 0, double.PositiveInfinity,
          new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Right,
          true, true, defaultProperties, TextWrapping.Wrap, 0, 0, 0));

                Assert.NotNull(textLine);
                Assert.NotEqual(0, textLine.NewLineLength);
            }
        }

        [Fact]
        public void Should_Get_TextBounds_With_Trailing_Zero_Advance()
        {
            const string df7Font = "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#DF7segHMI";

            using (Start())
            {
                var typeface = new Typeface(df7Font);
                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new SingleBufferTextSource("3,47-=?:#", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, 2);

                Assert.NotEmpty(textBounds);

                var textRunBounds = textBounds.First().TextRunBounds;

                Assert.NotEmpty(textBounds);

                var first = textRunBounds.First();

                Assert.Equal(0, first.TextSourceCharacterIndex);
                Assert.Equal(2, first.Length);
            }
        }

        [Fact]
        public void Should_Get_In_Cluster_Backspace_Hit()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope"));
                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new SingleBufferTextSource("ff", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var backspaceHit = textLine.GetBackspaceCaretCharacterHit(new CharacterHit(1, 1));

                Assert.Equal(1, backspaceHit.FirstCharacterIndex);
            }
        }

        private class TextHidden : TextRun
        {
            public TextHidden(int length)
            {
                Length = length;
            }

            public override int Length { get; }
        }

        private class CustomTextBufferTextSource : ITextSource
        {
            private IReadOnlyList<TextRun> _textRuns;

            public CustomTextBufferTextSource(params TextRun[] textRuns)
            {
                _textRuns = textRuns;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                var pos = 0;

                for (var i = 0; i < _textRuns.Count; i++)
                {
                    var currentRun = _textRuns[i];

                    if (pos + currentRun.Length > textSourceIndex)
                    {
                        return currentRun;
                    }

                    pos += currentRun.Length;
                }

                return null;
            }
        }

        private class MixedTextBufferTextSource : ITextSource
        {
            public TextRun? GetTextRun(int textSourceIndex)
            {
                switch (textSourceIndex)
                {
                    case 0:
                        return new TextCharacters("aaaaaaaaaa", new GenericTextRunProperties(Typeface.Default));
                    case 10:
                        return new TextCharacters("bbbbbbbbbb", new GenericTextRunProperties(Typeface.Default));
                    case 20:
                        return new TextCharacters("cccccccccc", new GenericTextRunProperties(Typeface.Default));
                    case 30:
                        return new TextCharacters("dddddddddd", new GenericTextRunProperties(Typeface.Default));
                    default:
                        return null;
                }
            }
        }

        private class DrawableRunTextSource : ITextSource
        {
            private const string Text = "_A_A";

            public TextRun? GetTextRun(int textSourceIndex)
            {
                switch (textSourceIndex)
                {
                    case 0:
                        return new CustomDrawableRun();
                    case 1:
                        return new TextCharacters(Text, new GenericTextRunProperties(Typeface.Default));
                    case 5:
                        return new CustomDrawableRun();
                    case 6:
                        return new TextCharacters(Text, new GenericTextRunProperties(Typeface.Default));
                    default:
                        return null;
                }
            }
        }

        private class CustomDrawableRun : DrawableTextRun
        {
            public override Size Size => new(14, 14);
            public override double Baseline => 14;
            public override void Draw(DrawingContext drawingContext, Point origin)
            {

            }
        }

        private static bool IsRightToLeft(TextLine textLine)
        {
            return textLine.TextRuns.Cast<ShapedTextRun>().Any(x => !x.ShapedBuffer.IsLeftToRight);
        }

        private static List<int> BuildGlyphClusters(TextLine textLine)
        {
            var glyphClusters = new List<int>();

            var shapedTextRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

            var lastCluster = -1;

            foreach (var textRun in shapedTextRuns)
            {
                var shapedBuffer = textRun.ShapedBuffer;

                var currentClusters = shapedBuffer.Select(glyph => glyph.GlyphCluster).ToList();

                foreach (var currentCluster in currentClusters)
                {
                    if (lastCluster == currentCluster)
                    {
                        continue;
                    }

                    glyphClusters.Add(currentCluster);

                    lastCluster = currentCluster;
                }
            }

            return glyphClusters;
        }

        private static List<Rect> BuildRects(TextLine textLine)
        {
            var rects = new List<Rect>();
            var height = textLine.Height;

            var currentX = 0d;

            var lastCluster = -1;

            var shapedTextRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

            foreach (var textRun in shapedTextRuns)
            {
                var shapedBuffer = textRun.ShapedBuffer;

                for (var index = 0; index < shapedBuffer.Length; index++)
                {
                    var currentCluster = shapedBuffer[index].GlyphCluster;

                    var advance = shapedBuffer[index].GlyphAdvance;

                    if (lastCluster != currentCluster)
                    {
                        rects.Add(new Rect(currentX, 0, advance, height));
                    }
                    else
                    {
                        var rect = rects[index - 1];

                        rects.Remove(rect);

                        rect = rect.WithWidth(rect.Width + advance);

                        rects.Add(rect);
                    }

                    currentX += advance;

                    lastCluster = currentCluster;
                }
            }

            return rects;
        }


        [Fact]
        public void Should_Get_TextBounds_Mixed()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var text = "0123";
                var shaperOption = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, 0, CultureInfo.CurrentCulture);

                var firstRun = new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties);

                var textRuns = new List<TextRun>
                {
                    new CustomDrawableRun(),
                    firstRun,
                    new CustomDrawableRun(),
                    new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties),
                    new CustomDrawableRun(),
                    new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties)
                };

                var textSource = new FixedRunsTextSource(textRuns);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, textLine.Length);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(0, 1);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(14, textBounds[0].Rectangle.Width);

                textBounds = textLine.GetTextBounds(0, firstRun.Length + 1);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(firstRun.Size.Width + 14, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(1, firstRun.Length);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(firstRun.Size.Width, textBounds[0].Rectangle.Width);

                textBounds = textLine.GetTextBounds(0, 1 + firstRun.Length);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(firstRun.Size.Width + 14, textBounds.Sum(x => x.Rectangle.Width));
            }
        }

        [Fact]
        public void Should_Get_TextBounds_BiDi_LeftToRight()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var text = "אאא AAA";
                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 200,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, 3);

                var firstRun = Assert.IsType<ShapedTextRun>(textLine.TextRuns[0]);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(firstRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(3, 4);

                var secondRun = Assert.IsType<ShapedTextRun>(textLine.TextRuns[1]);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(secondRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(0, 4);

                Assert.Equal(2, textBounds.Count);

                Assert.Equal(firstRun.Size.Width, textBounds[0].Rectangle.Width);

                Assert.Equal(7.201171875, textBounds[1].Rectangle.Width);

                Assert.Equal(firstRun.Size.Width, textBounds[1].Rectangle.Left);

                textBounds = textLine.GetTextBounds(0, text.Length);

                Assert.Equal(2, textBounds.Count);
                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width));
            }
        }

        [Fact]
        public void Should_Get_TextBounds_BiDi_RightToLeft()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var text = "אאא AAA";
                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 200,
                        new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, 4);

                var secondRun = Assert.IsType<ShapedTextRun>(textLine.TextRuns[1]);

                Assert.Equal(1, textBounds.Count);
                Assert.Equal(secondRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(4, 3);

                var firstRun = Assert.IsType<ShapedTextRun>(textLine.TextRuns[0]);

                Assert.Equal(1, textBounds.Count);

                Assert.Equal(3, textBounds[0].TextRunBounds.Sum(x => x.Length));
                Assert.Equal(firstRun.Size.Width, textBounds.Sum(x => x.Rectangle.Width));

                textBounds = textLine.GetTextBounds(0, 5);

                Assert.Equal(2, textBounds.Count);
                Assert.Equal(5, textBounds.Sum(x => x.TextRunBounds.Sum(x => x.Length)));

                Assert.Equal(secondRun.Size.Width, textBounds[1].Rectangle.Width);
                Assert.Equal(7.201171875, textBounds[0].Rectangle.Width);

                Assert.Equal(textLine.Start + 7.201171875, textBounds[0].Rectangle.Right, 2);
                Assert.Equal(textLine.Start + firstRun.Size.Width, textBounds[1].Rectangle.Left, 2);

                textBounds = textLine.GetTextBounds(0, text.Length);

                Assert.Equal(2, textBounds.Count);
                Assert.Equal(7, textBounds.Sum(x => x.TextRunBounds.Sum(x => x.Length)));
                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, textBounds.Sum(x => x.Rectangle.Width), 2);
            }
        }

        [Fact]
        public void Should_GetTextBounds_With_EndOfParagraph_RightToLeft()
        {
            var text = "لوحة المفاتيح العربية";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(0, 1);

                Assert.Equal(1, textBounds.Count);

                var firstBounds = textBounds.First();

                Assert.True(firstBounds.TextRunBounds.Count > 0);
            }
        }

        [Fact]
        public void Should_GetTextBounds_With_EndOfParagraph()
        {
            var text = "abc";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(3, 1);

                Assert.Equal(1, textBounds.Count);

                var firstBounds = textBounds.First();

                Assert.True(firstBounds.TextRunBounds.Count > 0);
            }
        }

        [Fact]
        public void Should_GetTextBounds_NotInfiniteLoop()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var shaperOption = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, 0, CultureInfo.CurrentCulture);
                var shaperOption2 = new TextShaperOptions(Typeface.Default.GlyphTypeface, 11, 0, CultureInfo.CurrentCulture);

                var textRuns = new List<TextRun>
                {
                    new ShapedTextRun(TextShaper.Current.ShapeText("قرأ ", shaperOption), defaultProperties),
                    new ShapedTextRun(TextShaper.Current.ShapeText("Wikipedia\u2122", shaperOption), defaultProperties),
                    new ShapedTextRun(TextShaper.Current.ShapeText("\u200e ", shaperOption2), defaultProperties),
                    new ShapedTextRun(TextShaper.Current.ShapeText("طوال اليوم", shaperOption), defaultProperties),
                    new ShapedTextRun(TextShaper.Current.ShapeText(".", shaperOption), defaultProperties)
                };

                var textSource = new FixedRunsTextSource(textRuns);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                            true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                textLine.GetTextBounds(4, 11);
            }
        }

        [Fact]
        public void Should_GetTextBounds_Bidi()
        {
            var text = "אבגדה 12345 ABCDEF אבגדה";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var bounds = textLine.GetTextBounds(6, 1);

                Assert.Equal(1, bounds.Count);

                Assert.Equal(0, bounds[0].Rectangle.Left);

                bounds = textLine.GetTextBounds(5, 1);

                Assert.Equal(1, bounds.Count);

                Assert.Equal(36.005859374999993, bounds[0].Rectangle.Left);

                bounds = textLine.GetTextBounds(0, 1);

                Assert.Equal(1, bounds.Count);

                Assert.Equal(71.165859375, bounds[0].Rectangle.Right);

                bounds = textLine.GetTextBounds(11, 1);

                Assert.Equal(1, bounds.Count);

                Assert.Equal(71.165859375, bounds[0].Rectangle.Left);

                bounds = textLine.GetTextBounds(0, 25);

                Assert.Equal(4, bounds.Count);

                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, bounds.Last().Rectangle.Right);
            }
        }

        [Fact]
        public void Should_GetTextBounds_Bidi_2()
        {
            var text = "אבג ABC אבג 123";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var bounds = textLine.GetTextBounds(0, text.Length);

                Assert.Equal(4, bounds.Count);

                var right = bounds.Last().Rectangle.Right;

                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, right);
            }
        }

        [Fact]
        public void Should_GetPreviousCharacterHit_Non_Trailing()
        {
            var text = "123.45.67.•";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(10, 1));
            }
        }

        [Theory]
        [InlineData("\0", 0.0)]
        [InlineData("\0\0\0", 0.0)]
        [InlineData("\0A\0\0", 7.201171875)]
        [InlineData("\0AA\0AA\0", 28.8046875)]
        public void Should_Ignore_Null_Terminator(string text, double width)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                Assert.Equal(width, textLine.Width);
            }
        }

        [Fact]
        public void Should_GetTextBounds_For_Clustered_Zero_Width_Characters()
        {
            const string text = "\r\n";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new TextFormatterTests.ListTextSource(new TextHidden(1), new TextCharacters(text, defaultProperties));

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(2, 1);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.NotEmpty(firstBounds.TextRunBounds);

                var firstRunBounds = firstBounds.TextRunBounds[0];

                Assert.Equal(2, firstRunBounds.TextSourceCharacterIndex);

                Assert.Equal(1, firstRunBounds.Length);
            }
        }

        [InlineData("y", -8, -1.304, -5.44)]
        [InlineData("f", -12, -11.824, -4.44)]
        [InlineData("a", 1, -0.232, -20.44)]
        [Win32Theory("Values depend on the Skia platform backend")]
        public void Should_Produce_Overhang(string text, double leading, double trailing, double after)
        {
            const string symbolsFont = "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Source Serif";

            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse(symbolsFont));

                var defaultProperties = new GenericTextRunProperties(typeface, 64);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                Assert.Equal(leading, textLine.OverhangLeading, 2);
                Assert.Equal(trailing, textLine.OverhangTrailing, 2);
                Assert.Equal(after, textLine.OverhangAfter, 2);
            }
        }

        [Fact]
        public void Should_GetTextBounds_For_Multiple_TextRuns()
        {
            var text = "Test👩🏽‍🚒";

            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface, 12);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var result = textLine.GetTextBounds(0, 11);

                Assert.Equal(1, result.Count);

                var firstBounds = result[0];

                Assert.NotEmpty(firstBounds.TextRunBounds);

                Assert.Equal(textLine.WidthIncludingTrailingWhitespace, firstBounds.Rectangle.Width, 2);
            }
        }

        [Fact]
        public void Should_GetTextBounds_Within_Cluster_2()
        {
            var text = "Test👩🏽‍🚒";

            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface, 12);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                var textPosition = 0;

                while (textPosition < text.Length)
                {
                    var bounds = textLine.GetTextBounds(textPosition, 1);

                    Assert.Equal(1, bounds.Count);

                    var firstBounds = bounds[0];

                    Assert.Equal(1, firstBounds.TextRunBounds.Count);

                    var firstRunBounds = firstBounds.TextRunBounds[0];

                    Assert.Equal(textPosition, firstRunBounds.TextSourceCharacterIndex);

                    var expectedDistance = firstRunBounds.Rectangle.Left;

                    var characterHit = new CharacterHit(textPosition);

                    var distance = textLine.GetDistanceFromCharacterHit(characterHit);

                    Assert.Equal(expectedDistance, distance, 2);

                    var nextCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);

                    var expectedNextPosition = textPosition + firstRunBounds.Length;

                    var nextPosition = nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength;

                    Assert.Equal(expectedNextPosition, nextPosition);

                    var previousCharacterHit = textLine.GetPreviousCaretCharacterHit(nextCharacterHit);

                    Assert.Equal(characterHit, previousCharacterHit);

                    textPosition += firstRunBounds.Length;
                }
            }
        }

        [Fact]
        public void Should_Get_TextBounds_With_Mixed_Runs_Within_Cluster()
        {
            using (Start())
            {
                const string manropeFont = "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope";

                var typeface = new Typeface(manropeFont);

                var defaultProperties = new GenericTextRunProperties(typeface);
                var text = "Fotografin";
                var shaperOption = new TextShaperOptions(typeface.GlyphTypeface);

                var firstRun = new ShapedTextRun(TextShaper.Current.ShapeText(text, shaperOption), defaultProperties);

                var textRuns = new List<TextRun>
                {
                    new CustomDrawableRun(),
                    new CustomDrawableRun(),
                    firstRun,
                    new CustomDrawableRun(),
                };

                var textSource = new FixedRunsTextSource(textRuns);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(10, 1);

                Assert.Equal(1, textBounds.Count);

                var firstBounds = textBounds[0];

                Assert.NotEmpty(firstBounds.TextRunBounds);

                var firstRunBounds = firstBounds.TextRunBounds[0];

                Assert.Equal(1, firstRunBounds.Length);
            }
        }

        [Fact]
        public void Should_Get_TextBounds_With_Glue()
        {
            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);
                var text = "a\u202C\u202C\u202C\u202Cb";

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(1, 1);

                Assert.NotEmpty(textBounds);

                var firstTextBounds = textBounds[0];

                Assert.NotEmpty(firstTextBounds.TextRunBounds);

                var firstRunBounds = firstTextBounds.TextRunBounds[0];

                Assert.Equal(1, firstRunBounds.TextSourceCharacterIndex);
                Assert.Equal(1, firstRunBounds.Length);
            }
        }

        [Fact]
        public void Should_Get_TextBounds_Tamil()
        {
            var text = "எடுத்துக்காட்டு வழி வினவல்";

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var textSource = new SingleBufferTextSource(text, defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left,
                        true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                Assert.NotEmpty(textLine.TextRuns);

                var firstRun = textLine.TextRuns[0] as ShapedTextRun;

                Assert.NotNull(firstRun);

                var clusterWidth = new List<double>();
                var distances = new List<double>();
                var clusters = new List<int>();
                var lastCluster = -1;
                var currentDistance = 0.0;
                var currentAdvance = 0.0;

                foreach (var glyphInfo in firstRun.ShapedBuffer)
                {
                    if (lastCluster != glyphInfo.GlyphCluster)
                    {
                        clusterWidth.Add(currentAdvance);
                        distances.Add(currentDistance);
                        clusters.Add(glyphInfo.GlyphCluster);

                        currentAdvance = 0;
                    }

                    lastCluster = glyphInfo.GlyphCluster;
                    currentDistance += glyphInfo.GlyphAdvance;
                    currentAdvance += glyphInfo.GlyphAdvance;
                }

                clusterWidth.RemoveAt(0);

                clusterWidth.Add(currentAdvance);

                for (var i = 6; i < clusters.Count; i++)
                {
                    var cluster = clusters[i];
                    var expectedDistance = distances[i];
                    var expectedWidth = clusterWidth[i];

                    var actualDistance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

                    Assert.Equal(expectedDistance, actualDistance, 2);

                    var characterHit = textLine.GetCharacterHitFromDistance(expectedDistance);

                    var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                    Assert.Equal(cluster, textPosition);

                    var bounds = textLine.GetTextBounds(cluster, 1);

                    Assert.NotNull(bounds);
                    Assert.NotEmpty(bounds);

                    var firstBounds = bounds[0];

                    Assert.NotEmpty(firstBounds.TextRunBounds);

                    var firstRunBounds = firstBounds.TextRunBounds[0];

                    Assert.Equal(cluster, firstRunBounds.TextSourceCharacterIndex);

                    var width = firstRunBounds.Rectangle.Width;

                    Assert.Equal(expectedWidth, width, 2);
                }
            }
        }

        [Fact]
        public void Should_GetTextBounds_Trailing_ZeroWidth()
        {
            var text = "dasdsad\r\n";

            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);
                var shaperOption = new TextShaperOptions(typeface.GlyphTypeface);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textBounds = textLine.GetTextBounds(7, 3);

                Assert.NotEmpty(textBounds);

                var firstBounds = textBounds[0];

                Assert.NotEmpty(firstBounds.TextRunBounds);

                var firstRunBounds = firstBounds.TextRunBounds[0];

                Assert.Equal(7, firstRunBounds.TextSourceCharacterIndex);

                Assert.Equal(2, firstRunBounds.Length);
            }
        }

        [Fact]
        public void Should_Add_Half_LineGap_To_Baseline()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Inter");
                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource("F", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var textMetrics = new TextMetrics(typeface.GlyphTypeface, 12);

                var expectedBaseline = -textMetrics.Ascent + textMetrics.LineGap / 2;

                Assert.Equal(expectedBaseline, textLine.Baseline);
            }
        }

        [Fact]
        public void Should_Clamp_Baseline_When_LineHeight_Is_Smaller_Than_Natural()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Inter");
                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource("F", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textMetrics = new TextMetrics(typeface.GlyphTypeface, 12);
                var natural = -textMetrics.Ascent + textMetrics.Descent + textMetrics.LineGap;

                var smallerLineHeight = natural - 2;

                // Force a smaller line height than ascent+descent+lineGap
                var paragraphProps = new GenericTextParagraphProperties(defaultProperties, lineHeight: smallerLineHeight);

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProps);

                Assert.NotNull(textLine);

                // In this case, baseline should equal -Ascent (lineGap ignored)
                var expectedBaseline = -textMetrics.Ascent;

                Assert.Equal(expectedBaseline, textLine.Baseline);
                Assert.Equal(paragraphProps.LineHeight, textLine.Height);
            }
        }

        [Fact]
        public void Should_Distribute_Extra_Space_When_LineHeight_Is_Larger_Than_Natural()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Inter");
                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource("F", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textMetrics = new TextMetrics(typeface.GlyphTypeface, 12);
                var natural = -textMetrics.Ascent + textMetrics.Descent + textMetrics.LineGap;

                var largerLineHeight = natural + 50;

                var paragraphProps = new GenericTextParagraphProperties(defaultProperties, lineHeight: largerLineHeight);

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProps);

                Assert.NotNull(textLine);

                // Extra space is distributed evenly above and below
                var extra = largerLineHeight - (textMetrics.Descent - textMetrics.Ascent);
                var expectedBaseline = -textMetrics.Ascent + extra / 2;

                Assert.Equal(expectedBaseline, textLine.Baseline, 5);
                Assert.Equal(largerLineHeight, textLine.Height, 5);
            }
        }

        [Fact]
        public void Backspace_Should_Treat_CRLF_As_A_Unit()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope"));
                var defaultProperties = new GenericTextRunProperties(typeface);
                var textSource = new SingleBufferTextSource("one\r\n", defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var backspaceHit = textLine.GetBackspaceCaretCharacterHit(new CharacterHit(5));

                Assert.Equal(3, backspaceHit.FirstCharacterIndex);
            }
        }

        [Fact]
        public void Should_Collapse_With_TextPathSegmentTrimming_Without_PathSegment()
        {
            var text = "foo";

            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var trimming = new TextPathSegmentTrimming("*");

                var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(15, defaultProperties, FlowDirection.LeftToRight));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.NotNull(collapsedLine);

                var result = ExtractTextFromRuns(collapsedLine);

                Assert.Equal("*o", result);
            }
        }

        [Fact]
        public void Should_Collapse_With_TextPathSegmentTrimming_NoSpace()
        {
            var text = "foo";

            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var trimming = new TextPathSegmentTrimming("*");

                var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(8, defaultProperties, FlowDirection.LeftToRight));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.NotNull(collapsedLine);

                var result = ExtractTextFromRuns(collapsedLine);

                Assert.Equal("*", result);
            }
        }

        [Theory]
        [InlineData("somedirectory\\")]
        [InlineData("somedirectory/")]
        public void TruncatePath_PathEndingWithSlash_ReturnsNonEmpty(string path)
        {
            var typeface = Typeface.Default;

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource(path, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var trimming = new TextPathSegmentTrimming("*");

                var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(50, defaultProperties, FlowDirection.LeftToRight));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.NotNull(collapsedLine);

                var result = ExtractTextFromRuns(collapsedLine);

                Assert.True(result.Contains("ory"));
            }
        }

        [Theory]
        [InlineData("directory\\file.txt")]
        [InlineData("directory/file.txt")]
        public void Should_Collapse_With_Ellipsis(string path)
        {
            var typeface = Typeface.Default;
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource(path, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var trimming = new TextPathSegmentTrimming("*");

                var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(8, defaultProperties, FlowDirection.LeftToRight));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.NotNull(collapsedLine);

                var result = ExtractTextFromRuns(collapsedLine);

                Assert.Equal("*", result);
            }
        }


        [Fact]
        public void Should_Trim_Path_At_The_End()
        {
            string text = "verylongdirectory\\file.txt";

            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var trimming = new TextPathSegmentTrimming("*");

                var collapsingProperties = trimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(40, defaultProperties, FlowDirection.LeftToRight));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.NotNull(collapsedLine);

                var result = ExtractTextFromRuns(collapsedLine);

                Assert.Equal("*.txt", result);
            }
        }

        public static string ExtractTextFromRuns(TextLine textLine)
        {
            // Only extract text for ShapedTextRun instances.
            return string.Concat(textLine.TextRuns
                .OfType<ShapedTextRun>()
                .Select(r => r.Text.ToString()));
        }

        private class FixedRunsTextSource : ITextSource
        {
            private readonly IReadOnlyList<TextRun> _textRuns;

            public FixedRunsTextSource(IReadOnlyList<TextRun> textRuns)
            {
                _textRuns = textRuns;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                var currentPosition = 0;

                foreach (var textRun in _textRuns)
                {
                    if (currentPosition == textSourceIndex)
                    {
                        return textRun;
                    }

                    currentPosition += textRun.Length;
                }

                return null;
            }
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
