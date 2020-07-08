using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextLineTests
    {
        [InlineData("𐐷𐐷𐐷𐐷𐐷")]
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

                for (var i = 1; i < clusters.Length; i++)
                {
                    nextCharacterHit = textLine.GetNextCaretCharacterHit(nextCharacterHit);

                    Assert.Equal(clusters[i], nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength);
                }
            }
        }

        [InlineData("𐐷𐐷𐐷𐐷𐐷")]
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

                var previousCharacterHit = new CharacterHit(clusters[^1]);

                for (var i = clusters.Length - 2; i > 0; i--)
                {
                    previousCharacterHit = textLine.GetPreviousCaretCharacterHit(previousCharacterHit);

                    Assert.Equal(clusters[i], previousCharacterHit.FirstCharacterIndex);
                }
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

                characterHit = textLine.GetCharacterHitFromDistance(textLine.LineMetrics.Size.Width);

                Assert.Equal(MultiBufferTextSource.TextRange.End, characterHit.FirstCharacterIndex);
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
