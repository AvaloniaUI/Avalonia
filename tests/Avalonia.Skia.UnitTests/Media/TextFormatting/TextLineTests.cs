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
                    textShaperImpl: new TextShaperImpl(),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
