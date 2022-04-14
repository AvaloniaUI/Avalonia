using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class GlyphRunTests
    {
        [InlineData("ABC012345", 0)] //LeftToRight
        [InlineData("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן", 1)] //RightToLeft
        [Theory]
        public void Should_Get_Next_CharacterHit(string text, sbyte direction)
        {
            using (Start())
            {
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, direction, CultureInfo.CurrentCulture);
                var shapedBuffer =
                    TextShaper.Current.ShapeText(text.AsMemory(), options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                var characterHit = new CharacterHit(0);
                var rects = BuildRects(glyphRun);

                if (glyphRun.IsLeftToRight)
                {
                    foreach (var rect in rects)
                    {
                        characterHit = glyphRun.GetNextCaretCharacterHit(characterHit);

                        var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

                        Assert.Equal(rect.Right, distance);
                    }
                }
                else
                {
                    shapedBuffer.GlyphInfos.Span.Reverse();

                    foreach (var rect in rects)
                    {
                        characterHit = glyphRun.GetNextCaretCharacterHit(characterHit);

                        var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

                        Assert.Equal(rect.Left, distance);
                    }
                }
            }
        }

        [InlineData("ABC012345", 0)] //LeftToRight
        [InlineData("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן", 1)] //RightToLeft
        [Theory]
        public void Should_Get_Previous_CharacterHit(string text, sbyte direction)
        {
            using (Start())
            {
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, direction, CultureInfo.CurrentCulture);
                var shapedBuffer =
                    TextShaper.Current.ShapeText(text.AsMemory(), options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                var characterHit = new CharacterHit(text.Length);
                var rects = BuildRects(glyphRun);

                rects.Reverse();
                
                if (glyphRun.IsLeftToRight)
                {
                    foreach (var rect in rects)
                    {
                        characterHit = glyphRun.GetPreviousCaretCharacterHit(characterHit);

                        var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

                        Assert.Equal(rect.Left, distance);
                    }
                }
                else
                {
                    shapedBuffer.GlyphInfos.Span.Reverse();

                    foreach (var rect in rects)
                    {
                        characterHit = glyphRun.GetPreviousCaretCharacterHit(characterHit);

                        var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

                        Assert.Equal(rect.Right, distance);
                    }
                }
            }
        }
        
        [InlineData("ABC012345", 0)] //LeftToRight
        [InlineData("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן", 1)] //RightToLeft
        [Theory]
        public void Should_Get_CharacterHit_From_Distance(string text, sbyte direction)
        {
            using (Start())
            {
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, direction, CultureInfo.CurrentCulture);
                var shapedBuffer =
                   TextShaper.Current.ShapeText(text.AsMemory(), options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                if (glyphRun.IsLeftToRight)
                {
                    var characterHit =
                        glyphRun.GetCharacterHitFromDistance(glyphRun.Metrics.WidthIncludingTrailingWhitespace, out _);
                    
                    Assert.Equal(glyphRun.Characters.Length, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
                }
                else
                {
                    shapedBuffer.GlyphInfos.Span.Reverse();

                    var characterHit =
                        glyphRun.GetCharacterHitFromDistance(0, out _);
                    
                    Assert.Equal(glyphRun.Characters.Length, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
                }
                
                var rects = BuildRects(glyphRun);
                
                var lastCluster = -1;
                var index = 0;

                if (!glyphRun.IsLeftToRight)
                {
                    rects.Reverse();
                }

                foreach (var rect in rects)
                {
                    var currentCluster = glyphRun.GlyphClusters[index];

                    while (currentCluster == lastCluster && index + 1 < glyphRun.GlyphClusters.Count)
                    {
                        currentCluster = glyphRun.GlyphClusters[++index];
                    }

                    //Non trailing edge
                    var distance = glyphRun.IsLeftToRight ? rect.Left : rect.Right;

                    var characterHit = glyphRun.GetCharacterHitFromDistance(distance, out _);

                    Assert.Equal(currentCluster, characterHit.FirstCharacterIndex + characterHit.TrailingLength);

                    lastCluster = currentCluster;

                    index++;
                }
            }
        }

        private static List<Rect> BuildRects(GlyphRun glyphRun)
        {
            var height = glyphRun.Size.Height;

            var currentX = glyphRun.IsLeftToRight ? 0d : glyphRun.Metrics.WidthIncludingTrailingWhitespace;
            
            var rects = new List<Rect>(glyphRun.GlyphAdvances!.Count);

            var lastCluster = -1;

            for (var index = 0; index < glyphRun.GlyphAdvances.Count; index++)
            {
                var currentCluster = glyphRun.GlyphClusters![index];
                
                var advance = glyphRun.GlyphAdvances[index];

                if (lastCluster != currentCluster)
                {
                    if (glyphRun.IsLeftToRight)
                    {
                        rects.Add(new Rect(currentX, 0, advance, height));
                    }
                    else
                    {
                        rects.Add(new Rect(currentX - advance, 0, advance, height));
                    }
                }
                else
                {
                    var rect = rects[index - 1];

                    rects.Remove(rect);

                    rect = glyphRun.IsLeftToRight ? 
                        rect.WithWidth(rect.Width + advance) : 
                        new Rect(rect.X - advance, 0, rect.Width + advance, height);
                    
                   rects.Add(rect);
                }

                if (glyphRun.IsLeftToRight)
                {
                    currentX += advance;
                }
                else
                {
                    currentX -= advance;
                }

                lastCluster = currentCluster;
            }

            return rects;
        }

        private static GlyphRun CreateGlyphRun(ShapedBuffer shapedBuffer)
        {
            return new GlyphRun(
                shapedBuffer.GlyphTypeface,
                shapedBuffer.FontRenderingEmSize,
                shapedBuffer.Text,
                shapedBuffer.GlyphIndices,
                shapedBuffer.GlyphAdvances,
                shapedBuffer.GlyphOffsets,
                shapedBuffer.GlyphClusters,
                shapedBuffer.BidiLevel);
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
