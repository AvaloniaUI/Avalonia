using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
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

        [Fact]
        public void Should_Get_Distance_From_CharacterHit_Non_Trailing_RightToLeft()
        {
            const string text = "נִקּוּד";

            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Inter");
                var options = new TextShaperOptions(typeface.GlyphTypeface, 14, 1);
                var shapedBuffer = TextShaper.Current.ShapeText(text, options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                //Get the distance to the left side of the first glyph
                var actualDistance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(text.Length));

                var expectedDistance = 0.0;

                Assert.Equal(expectedDistance, actualDistance, 2);

                //Get the distance to the right side of the first glyph
                actualDistance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(text.Length - 1));

                expectedDistance = shapedBuffer[0].GlyphAdvance;

                Assert.Equal(expectedDistance, actualDistance, 2);
            }
        }

        [Fact]
        public void Should_CharacterHit_From_Distance_Zero_Width()
        {
            const string df7Font = "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#DF7segHMI";
            const string text = "3,47-=?:#";

            using (Start())
            {
                var typeface = new Typeface(df7Font);
                var options = new TextShaperOptions(typeface.GlyphTypeface, 14, 0);
                var shapedBuffer = TextShaper.Current.ShapeText(text, options);

                Assert.NotEmpty(shapedBuffer);

                var firstGlyphInfo = shapedBuffer[0];

                var glyphRun = CreateGlyphRun(shapedBuffer);

                var characterHit = glyphRun.GetCharacterHitFromDistance(firstGlyphInfo.GlyphAdvance, out _);

                Assert.Equal(2, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit_Zero_Width()
        {
            const string text = "נִקּוּד";

            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Inter");
                var options = new TextShaperOptions(typeface.GlyphTypeface, 14, 1);
                var shapedBuffer = TextShaper.Current.ShapeText(text, options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                var clusters = glyphRun.GlyphInfos.GroupBy(x => x.GlyphCluster);

                var rightSideDistances = new List<double>();

                var leftSideDistances = new List<double>();

                var currentX = 0.0;

                foreach (var cluster in clusters)
                {
                    leftSideDistances.Add(currentX);

                    currentX += cluster.Sum(x => x.GlyphAdvance);

                    rightSideDistances.Add(currentX);
                }

                var characterIndices = clusters.Select(x => x.First().GlyphCluster).ToList();

                var characterHit = new CharacterHit(text.Length);

                for (var i = 0; i < characterIndices.Count; i++)
                {
                    var characterIndex = characterIndices[i];

                    var leftSideDistance = leftSideDistances[i];

                    var leftSideCharacterHit = glyphRun.GetCharacterHitFromDistance(leftSideDistance, out _);

                    var distance = glyphRun.GetDistanceFromCharacterHit(leftSideCharacterHit);

                    Assert.Equal(leftSideDistance, distance, 2);

                    var previousCharacterHit = glyphRun.GetPreviousCaretCharacterHit(characterHit);

                    distance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(characterIndex));

                    var rightSideDistance = rightSideDistances[i];

                    Assert.Equal(rightSideDistance, distance, 2);

                    characterHit = previousCharacterHit;
                }
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit_Within_Cluster()
        {
            var text = "எடுத்துக்காட்டு வழி வினவல்";

            using (Start())
            {
                var cp = Codepoint.ReadAt(text, 0, out _);

                Assert.True(FontManager.Current.TryMatchCharacter(cp, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var typeface));

                var options = new TextShaperOptions(typeface.GlyphTypeface, 12);

                var shapedBuffer = TextShaper.Current.ShapeText(text, options);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                var clusterWidth = new List<double>();
                var distances = new List<double>();
                var clusters = new List<int>();
                var lastCluster = -1;
                var currentDistance = 0.0;
                var currentAdvance = 0.0;

                foreach (var glyphInfo in shapedBuffer)
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

                var expectedLeftHit = new CharacterHit(11);

                var distance = glyphRun.GetDistanceFromCharacterHit(expectedLeftHit);

                var expectedLeft = distances[6];

                Assert.Equal(expectedLeft, distance);

                var leftHit = glyphRun.GetCharacterHitFromDistance(expectedLeft, out _);

                Assert.Equal(11, leftHit.FirstCharacterIndex + leftHit.TrailingLength);

                var expectedRight = distances[7];

                distance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(12));

                Assert.Equal(expectedRight, distance);

                var expectedRightHit = new CharacterHit(13);

                distance = glyphRun.GetDistanceFromCharacterHit(expectedRightHit);

                Assert.Equal(expectedRight, distance);

                var rightHit = glyphRun.GetCharacterHitFromDistance(expectedRight, out _);

                Assert.Equal(13, rightHit.FirstCharacterIndex + rightHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Add_Half_LineGap_To_Baseline()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Inter");
                var options = new TextShaperOptions(typeface.GlyphTypeface, 14);
                var shapedBuffer = TextShaper.Current.ShapeText("F", options);

                var textMetrics = new TextMetrics(shapedBuffer.GlyphTypeface, 14);

                var glyphRun = CreateGlyphRun(shapedBuffer);

                var expectedBaseline = -textMetrics.Ascent + textMetrics.LineGap / 2;

                Assert.Equal(expectedBaseline, glyphRun.Metrics.Baseline);
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
            var glyphRun = new GlyphRun(
                shapedBuffer.GlyphTypeface,
                shapedBuffer.FontRenderingEmSize,
                shapedBuffer.Text,
                shapedBuffer,
                biDiLevel: shapedBuffer.BidiLevel);

            if (shapedBuffer.BidiLevel == 1)
            {
                shapedBuffer.Reverse();
            }

            return glyphRun;
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
