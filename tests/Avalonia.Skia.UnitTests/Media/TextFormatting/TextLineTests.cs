using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Avalonia.Utilities;
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

                var clusters = new List<int>();

                foreach (var textRun in textLine.TextRuns.OrderBy(x=> x.Text.Start))
                {
                    var shapedRun = (ShapedTextCharacters)textRun;

                    clusters.AddRange(shapedRun.IsReversed ?
                        shapedRun.ShapedBuffer.GlyphClusters.Reverse() :
                        shapedRun.ShapedBuffer.GlyphClusters);
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

                var clusters = new List<int>();

                foreach (var textRun in textLine.TextRuns.OrderBy(x=> x.Text.Start))
                {
                    var shapedRun = (ShapedTextCharacters)textRun;

                    clusters.AddRange(shapedRun.IsReversed ?
                        shapedRun.ShapedBuffer.GlyphClusters.Reverse() :
                        shapedRun.ShapedBuffer.GlyphClusters);
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

                var clusters = textLine.TextRuns.Cast<ShapedTextCharacters>().SelectMany(x => x.ShapedBuffer.GlyphClusters)
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

                var clusters = textLine.TextRuns.Cast<ShapedTextCharacters>().SelectMany(x => x.ShapedBuffer.GlyphClusters)
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

                var textSource = new SingleBufferTextSource(s_multiLineText, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                var currentDistance = 0.0;

                foreach (var run in textLine.TextRuns)
                {
                    var textRun = (ShapedTextCharacters)run;

                    var glyphRun = textRun.GlyphRun;

                    for (var i = 0; i < glyphRun.GlyphClusters!.Count; i++)
                    {
                        var cluster = glyphRun.GlyphClusters[i];

                        var advance = glyphRun.GlyphAdvances[i];

                        var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

                        Assert.Equal(currentDistance, distance);

                        currentDistance += advance;
                    }
                }

                Assert.Equal(currentDistance,textLine.GetDistanceFromCharacterHit(new CharacterHit(s_multiLineText.Length)));
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

        [InlineData("01234 01234", 58, TextCollapsingStyle.TrailingCharacter, "01234 0\u2026")]
        [InlineData("01234 01234", 58, TextCollapsingStyle.TrailingWord, "01234\u2026")]
        [InlineData("01234", 9, TextCollapsingStyle.TrailingCharacter, "\u2026")]
        [InlineData("01234", 2, TextCollapsingStyle.TrailingCharacter, "")]
        [Theory]
        public void Should_Collapse_Line(string text, double width, TextCollapsingStyle style, string expected)
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

        private static bool IsRightToLeft(TextLine textLine)
        {
            return textLine.TextRuns.Cast<ShapedTextCharacters>().Any(x => !x.ShapedBuffer.IsLeftToRight);
        }

        private static List<int> BuildGlyphClusters(TextLine textLine)
        {
            var glyphClusters = new List<int>();

            var shapedTextRuns = textLine.TextRuns.Cast<ShapedTextCharacters>().ToList();

            var lastCluster = -1;
            
            foreach (var textRun in shapedTextRuns)
            {
                var shapedBuffer = textRun.ShapedBuffer;

                var currentClusters = shapedBuffer.GlyphClusters.ToList();

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

            var shapedTextRuns = textLine.TextRuns.Cast<ShapedTextCharacters>().ToList();

            foreach (var textRun in shapedTextRuns)
            {
                var shapedBuffer = textRun.ShapedBuffer;
            
                for (var index = 0; index < shapedBuffer.GlyphAdvances.Count; index++)
                {
                    var currentCluster = shapedBuffer.GlyphClusters[index];
                
                    var advance = shapedBuffer.GlyphAdvances[index];

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
