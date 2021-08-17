using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextLineTests
    {
        private static readonly string s_multiLineText = "012345678\r\r0123456789";

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

                    var firstCharacterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(int.MinValue));

                    Assert.Equal(textLine.TextRange.Start, firstCharacterHit.FirstCharacterIndex);

                    currentIndex += textLine.TextRange.Length;
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

                    var lastCharacterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(int.MaxValue));

                    Assert.Equal(textLine.TextRange.Start + textLine.TextRange.Length,
                        lastCharacterHit.FirstCharacterIndex + lastCharacterHit.TrailingLength);

                    currentIndex += textLine.TextRange.Length;
                }
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

                var clusters = textLine.TextRuns.Cast<ShapedTextCharacters>().SelectMany(x => x.GlyphRun.GlyphClusters)
                    .ToArray();

                var nextCharacterHit = new CharacterHit(0);

                for (var i = 0; i < clusters.Length; i++)
                {
                    Assert.Equal(clusters[i], nextCharacterHit.FirstCharacterIndex);

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

                var clusters = textLine.TextRuns.Cast<ShapedTextCharacters>().SelectMany(x => x.GlyphRun.GlyphClusters)
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

                firstCharacterHit = previousCharacterHit;

                previousCharacterHit = textLine.GetPreviousCaretCharacterHit(firstCharacterHit);

                Assert.Equal(firstCharacterHit.FirstCharacterIndex, previousCharacterHit.FirstCharacterIndex);

                Assert.Equal(0, previousCharacterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new MultiBufferTextSource(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                var currentDistance = 0.0;

                foreach (var run in textLine.TextRuns)
                {
                    var textRun = (ShapedTextCharacters)run;

                    var glyphRun = textRun.GlyphRun;

                    for (var i = 0; i < glyphRun.GlyphClusters.Length; i++)
                    {
                        var cluster = glyphRun.GlyphClusters[i];

                        var glyph = glyphRun.GlyphIndices[i];

                        var advance = glyphRun.GlyphTypeface.GetGlyphAdvance(glyph) * glyphRun.Scale;

                        var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

                        Assert.Equal(currentDistance, distance);

                        currentDistance += advance;
                    }
                }

                Assert.Equal(currentDistance,
                    textLine.GetDistanceFromCharacterHit(new CharacterHit(MultiBufferTextSource.TextRange.Length)));
            }
        }

        [Fact]
        public void Should_Get_CharacterHit_From_Distance()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new MultiBufferTextSource(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                var currentDistance = 0.0;

                CharacterHit characterHit;

                foreach (var run in textLine.TextRuns)
                {
                    var textRun = (ShapedTextCharacters)run;

                    var glyphRun = textRun.GlyphRun;

                    for (var i = 0; i < glyphRun.GlyphClusters.Length; i++)
                    {
                        var cluster = glyphRun.GlyphClusters[i];

                        var glyph = glyphRun.GlyphIndices[i];

                        var advance = glyphRun.GlyphTypeface.GetGlyphAdvance(glyph) * glyphRun.Scale;

                        characterHit = textLine.GetCharacterHitFromDistance(currentDistance);

                        Assert.Equal(cluster, characterHit.FirstCharacterIndex + characterHit.TrailingLength);

                        currentDistance += advance;
                    }
                }

                characterHit = textLine.GetCharacterHitFromDistance(textLine.Width);

                Assert.Equal(MultiBufferTextSource.TextRange.End, characterHit.FirstCharacterIndex);
            }
        }

        [InlineData("01234 01234", 8, TextCollapsingStyle.TrailingCharacter, "01234 0\u2026")]
        [InlineData("01234 01234", 8, TextCollapsingStyle.TrailingWord, "01234\u2026")]
        [Theory]
        public void Should_Collapse_Line(string text, int numberOfCharacters, TextCollapsingStyle style, string expected)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.False(textLine.HasCollapsed);

                var glyphTypeface = Typeface.Default.GlyphTypeface;

                var scale = defaultProperties.FontRenderingEmSize / glyphTypeface.DesignEmHeight;

                var width = 1.0;

                for (var i = 0; i < numberOfCharacters; i++)
                {
                    var glyph = glyphTypeface.GetGlyph(text[i]);

                    width += glyphTypeface.GetGlyphAdvance(glyph) * scale;
                }

                TextCollapsingProperties collapsingProperties;

                if (style == TextCollapsingStyle.TrailingCharacter)
                {
                    collapsingProperties = new TextTrailingCharacterEllipsis(width, defaultProperties);
                }
                else
                {
                    collapsingProperties = new TextTrailingWordEllipsis(width, defaultProperties);
                }

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.True(collapsedLine.HasCollapsed);

                var trimmedText = collapsedLine.TextRuns.SelectMany(x => x.Text).ToArray();

                Assert.Equal(expected.Length, trimmedText.Length);

                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.Equal(expected[i], trimmedText[i]);
                }
            }
        }

        [Fact(Skip = "Verify this")]
        public void Should_Ignore_NewLine_Characters()
        {
            using (Start())
            {
                var defaultTextRunProperties =
                    new GenericTextRunProperties(Typeface.Default);

                const string text = "01234567🎉\n";

                var source = new SingleBufferTextSource(text, defaultTextRunProperties);

                var textParagraphProperties = new GenericTextParagraphProperties(defaultTextRunProperties);

                var formatter = TextFormatter.Current;

                var textLine = formatter.FormatLine(source, 0, double.PositiveInfinity, textParagraphProperties);

                var nextCharacterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(8, 2));

                Assert.Equal(new CharacterHit(8, 2), nextCharacterHit);
            }
        }
        
        [Fact]
        public void TextLineBreak_Should_Contain_TextEndOfLine()
        {
            using (Start())
            {
                var defaultTextRunProperties =
                    new GenericTextRunProperties(Typeface.Default);

                const string text = "0123456789";

                var source = new SingleBufferTextSource(text, defaultTextRunProperties);

                var textParagraphProperties = new GenericTextParagraphProperties(defaultTextRunProperties);

                var formatter = TextFormatter.Current;

                var textLine = formatter.FormatLine(source, 0, double.PositiveInfinity, textParagraphProperties);

                Assert.NotNull(textLine.TextLineBreak.TextEndOfLine);
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
