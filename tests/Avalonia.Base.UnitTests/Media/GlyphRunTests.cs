using System;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphRunTests : TestWithServicesBase
    {
        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 0, 0, 0)]
        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 0, 3, 30)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 1, 0, 10)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 2, 0, 20)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 2, 1, 30)]
        [Theory]
        public void Should_Get_Distance_From_CharacterHit(double[] advances, int[] clusters, int start, int trailingLength, double expectedDistance)
        {
            using (Start())
            using (var glyphRun = CreateGlyphRun(advances, clusters))
            {
                var characterHit = new CharacterHit(start, trailingLength);

                var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

                Assert.Equal(expectedDistance, distance);
            }
        }

        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 26.0, 0, 3, true)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 20.0, 2, 0, true)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 26.0, 2, 1, true)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 35.0, 2, 1, false)]
        [Theory]
        public void Should_Get_CharacterHit_FromDistance(double[] advances, int[] clusters, double distance, int start,
            int trailingLengthExpected, bool isInsideExpected)
        {
            using (Start())
            using (var glyphRun = CreateGlyphRun(advances, clusters))
            {
                var textBounds = glyphRun.GetCharacterHitFromDistance(distance, out var isInside);

                Assert.Equal(start, textBounds.FirstCharacterIndex);

                Assert.Equal(trailingLengthExpected, textBounds.TrailingLength);

                Assert.Equal(isInsideExpected, isInside);
            }
        }

        [InlineData(new double[] { 10, 10, 10 }, new int[] { 10, 11, 12 }, 0, -1, 10, 1, 10)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 10, 11, 12 }, 0, 15, 12, 1, 10)]
        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 0, 0, 0, 3, 30.0)]
        [InlineData(new double[] { 10, 10, 10 }, new int[] { 0, 1, 2 }, 0, 1, 1, 1, 10.0)]
        [InlineData(new double[] { 10, 20, 0, 10 }, new int[] { 0, 1, 1, 3 }, 0, 2, 1, 2, 20.0)]
        [InlineData(new double[] { 10, 20, 0, 10 }, new int[] { 0, 1, 1, 3 }, 0, 1, 1, 2, 20.0)]
        [InlineData(new double[] { 10, 0, 20, 10 }, new int[] { 3, 1, 1, 0 }, 1, 1, 1, 2, 20.0)]
        [Theory]
        public void Should_Find_Nearest_CharacterHit(double[] advances, int[] clusters, int bidiLevel,
            int index, int expectedIndex, int expectedLength, double expectedWidth)
        {
            using(UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
            {
                var textBounds = glyphRun.FindNearestCharacterHit(index, out var width);

                Assert.Equal(expectedIndex, textBounds.FirstCharacterIndex);

                Assert.Equal(expectedLength, textBounds.TrailingLength);

                Assert.Equal(expectedWidth, width, 2);
            }
        }

        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 0, 0, 0, 3, 0)]
        [InlineData(new double[] { 0, 0, 30 }, new int[] { 0, 0, 0 }, 0, 0, 0, 3, 1)]
        [InlineData(new double[] { 30, 0, 0, 10 }, new int[] { 0, 0, 0, 3 }, 3, 0, 3, 1, 0)]
        [InlineData(new double[] { 10, 0, 0, 30 }, new int[] { 3, 0, 0, 0 }, 3, 0, 3, 1, 1)]
        [InlineData(new double[] { 10, 30, 0, 0, 10 }, new int[] { 0, 1, 1, 1, 4 }, 1, 0, 4, 0, 0)]
        [InlineData(new double[] { 10, 0, 0, 30, 10 }, new int[] { 4, 1, 1, 1, 0 }, 1, 0, 4, 0, 1)]
        [Theory]
        public void Should_Get_Next_CharacterHit(double[] advances,int[] clusters,
            int firstCharacterIndex, int trailingLength,
            int nextIndex, int nextLength,
            int bidiLevel)
        {
            using(UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
            {
                var characterHit = glyphRun.GetNextCaretCharacterHit(new CharacterHit(firstCharacterIndex, trailingLength));

                Assert.Equal(nextIndex, characterHit.FirstCharacterIndex);

                Assert.Equal(nextLength, characterHit.TrailingLength);
            }
        }

        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 0, 0, 0, 0, 0)]
        [InlineData(new double[] { 0, 0, 30 }, new int[] { 0, 0, 0 }, 0, 0, 0, 0, 1)]
        [InlineData(new double[] { 30, 0, 0, 10 }, new int[] { 0, 0, 0, 3 }, 3, 1, 3, 0, 0)]
        [InlineData(new double[] { 0, 0, 30, 10 }, new int[] { 3, 0, 0, 0 }, 3, 1, 3, 0, 1)]
        [InlineData(new double[] { 10, 30, 0, 0, 10 }, new int[] { 0, 1, 1, 1, 4 }, 4, 1, 4, 0, 0)]
        [InlineData(new double[] { 10, 0, 0, 30, 10 }, new int[] { 4, 1, 1, 1, 0 }, 4, 1, 4, 0, 1)]
        [Theory]
        public void Should_Get_Previous_CharacterHit(double[] advances, int[] clusters,
            int currentIndex, int currentLength,
            int previousIndex, int previousLength,
            int bidiLevel)
        {
            using(UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
            {
                var characterHit = glyphRun.GetPreviousCaretCharacterHit(new CharacterHit(currentIndex + currentLength));

                Assert.Equal(previousIndex, characterHit.FirstCharacterIndex);

                Assert.Equal(previousLength, characterHit.TrailingLength);
            }
        }

        [InlineData(new double[] { 30, 0, 0 }, new int[] { 0, 0, 0 }, 0)]
        [InlineData(new double[] { 0, 0, 30 }, new int[] { 0, 0, 0 }, 1)]
        [InlineData(new double[] { 10, 10, 10, 10 }, new int[] { 0, 0, 0, 3 }, 0)]
        [InlineData(new double[] { 10, 10, 10, 10 }, new int[] { 3, 0, 0, 0 }, 1)]
        [InlineData(new double[] { 10, 10, 10, 10, 10 }, new int[] { 0, 1, 1, 1, 4 }, 0)]
        [InlineData(new double[] { 10, 10, 10, 10, 10 }, new int[] { 4, 1, 1, 1, 0 }, 1)]
        [Theory]
        public void Should_Find_Glyph_Index(double[] advances, int[] clusters, int bidiLevel)
        {
            using(UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            using (var glyphRun = CreateGlyphRun(advances, clusters, bidiLevel))
            {
                if (glyphRun.IsLeftToRight)
                {
                    for (var i = 0; i < clusters.Length; i++)
                    {
                        var cluster = clusters[i];

                        var found = glyphRun.FindGlyphIndex(cluster);

                        var expected = i;

                        while (expected - 1 >= 0 && clusters[expected - 1] == cluster)
                        {
                            expected--;
                        }

                        Assert.Equal(expected, found);
                    }
                }
                else
                {
                    for (var i = clusters.Length - 1; i > 0; i--)
                    {
                        var cluster = clusters[i];

                        var found = glyphRun.FindGlyphIndex(cluster);

                        var expected = i;

                        while (expected + 1 < clusters.Length && clusters[expected + 1] == cluster)
                        {
                            expected++;
                        }

                        Assert.Equal(expected, found);
                    }
                }
            }
        }

        // A run can hold characters and no glyphs at all: a line break in a font that gives the shaper
        // no way to hide it shapes to nothing. Hit-testing has no cluster to snap to there, so it must
        // degenerate to the run's single zero-width position instead of indexing an empty glyph list.
        [Fact]
        public void Should_Hit_Test_Run_Without_Glyphs()
        {
            using (Start())
            using (var glyphRun = new GlyphRun(Typeface.Default.GlyphTypeface, 10, "\r\n".AsMemory(),
                       Array.Empty<GlyphInfo>()))
            {
                Assert.Equal(0, glyphRun.Bounds.Width);

                // Answered without building a platform glyph run for something that marks nothing.
                Assert.Equal(default, glyphRun.InkBounds);

                Assert.Equal(0, glyphRun.GetDistanceFromCharacterHit(new CharacterHit(0)));
                Assert.Equal(0, glyphRun.GetDistanceFromCharacterHit(new CharacterHit(0, 2)));

                Assert.Equal(0, glyphRun.FindGlyphIndex(0));

                var nearestHit = glyphRun.FindNearestCharacterHit(0, out var width);

                Assert.Equal(0, nearestHit.FirstCharacterIndex);
                Assert.Equal(2, nearestHit.TrailingLength);
                Assert.Equal(0, width);

                var hitFromDistance = glyphRun.GetCharacterHitFromDistance(0, out var isInside);

                Assert.False(isInside);
                Assert.Equal(0, hitFromDistance.FirstCharacterIndex);
            }
        }

        private static GlyphRun CreateGlyphRun(double[] glyphAdvances, int[] glyphClusters, int bidiLevel = 0)
        {
            var count = glyphAdvances.Length;

            var glyphInfos = new GlyphInfo[count];
            for (var i = 0; i < count; ++i)
            {
                glyphInfos[i] = new GlyphInfo(0, glyphClusters[i], glyphAdvances[i]);
            }

            return new GlyphRun(Typeface.Default.GlyphTypeface, 10, new string('a', count).AsMemory(), glyphInfos, biDiLevel: bidiLevel);
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(
                renderInterface: new HeadlessPlatformRenderInterface()));
        }
    }
}
