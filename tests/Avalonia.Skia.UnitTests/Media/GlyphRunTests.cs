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
                    TextShaper.Current.ShapeText(text, options);

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
                    TextShaper.Current.ShapeText(text, options);

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
                   TextShaper.Current.ShapeText(text, options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                if (glyphRun.IsLeftToRight)
                {
                    var characterHit =
                        glyphRun.GetCharacterHitFromDistance(glyphRun.Bounds.Width, out _);
                    
                    Assert.Equal(glyphRun.Characters.Length, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
                }
                else
                {
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
                    var currentCluster = glyphRun.GlyphInfos[index].GlyphCluster;

                    while (currentCluster == lastCluster && index + 1 < glyphRun.GlyphInfos.Count)
                    {
                        currentCluster = glyphRun.GlyphInfos[++index].GlyphCluster;
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

        // https://github.com/AvaloniaUI/Avalonia/issues/12676
        [Fact]
        public void Similar_Runs_Have_Same_InkBounds_After_Blob_Creation()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Inter");
                var options = new TextShaperOptions(typeface.GlyphTypeface, 14);
                var shapedBuffer = TextShaper.Current.ShapeText("F", options);

                var glyphRun1 = CreateGlyphRun(shapedBuffer);
                var bounds1 = glyphRun1.InkBounds;
                ((GlyphRunImpl)glyphRun1.PlatformImpl.Item).GetTextBlob(new RenderOptions { TextRenderingMode = TextRenderingMode.SubpixelAntialias });

                var bounds2 = CreateGlyphRun(shapedBuffer).InkBounds;

                Assert.Equal(bounds1, bounds2);
            }
        }

        [Fact]
        public void GlyphRun_With_Leading_Space_Has_Correct_InkBounds()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Inter");
                var options = new TextShaperOptions(typeface.GlyphTypeface, 14);
                var shapedBuffer = TextShaper.Current.ShapeText(" I", options);

                var glyphRun1 = CreateGlyphRun(shapedBuffer);
                var bounds = glyphRun1.InkBounds;

                Assert.True(bounds.Left > 0);
            }
        }

        private static List<Rect> BuildRects(GlyphRun glyphRun)
        {
            var height = glyphRun.Bounds.Height;

            var currentX = glyphRun.IsLeftToRight ? 0d : glyphRun.Bounds.Width;
            
            var rects = new List<Rect>(glyphRun.GlyphInfos!.Count);

            var lastCluster = -1;

            for (var index = 0; index < glyphRun.GlyphInfos.Count; index++)
            {
                var currentCluster = glyphRun.GlyphInfos[index].GlyphCluster;
                
                var advance = glyphRun.GlyphInfos[index].GlyphAdvance;

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
            var glyphRun =  new GlyphRun(
                shapedBuffer.GlyphTypeface,
                shapedBuffer.FontRenderingEmSize,
                shapedBuffer.Text,
                shapedBuffer,
                biDiLevel: shapedBuffer.BidiLevel);

            if(shapedBuffer.BidiLevel == 1)
            {
                shapedBuffer.Reverse();
            }

            return glyphRun;
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(),
                    textShaperImpl: new TextShaperImpl(),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
