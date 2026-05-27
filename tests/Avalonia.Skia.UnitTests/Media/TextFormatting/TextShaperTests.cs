using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextShaperTests
    {
        [Fact]
        public void Should_Form_Clusters_For_BreakPairs()
        {
            using (Start())
            {
                var text = "\n\r\n";
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12,0, CultureInfo.CurrentCulture);
                var shapedBuffer = TextShaper.Current.ShapeText(text, options);
                
                Assert.Equal(shapedBuffer.Length, text.Length);
                Assert.Equal(shapedBuffer.Length, text.Length);
                Assert.Equal(0, shapedBuffer[0].GlyphCluster);
                Assert.Equal(1, shapedBuffer[1].GlyphCluster);
                Assert.Equal(1, shapedBuffer[2].GlyphCluster);
            }
        }

        [Fact]
        public void Should_Apply_IncrementalTabWidth()
        {
            using (Start())
            {
                var text = "012345\t";
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture, 100);
                var shapedBuffer = TextShaper.Current.ShapeText(text.AsMemory().Slice(6), options);

                Assert.Equal(1, shapedBuffer.Length);
                Assert.Equal(100, shapedBuffer[0].GlyphAdvance);
            }
        }

        [Fact]
        public void Should_Not_Split_Cluster()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Cascadia Code"));
         
                var buffer = TextShaper.Current.ShapeText("a\"๊a", new TextShaperOptions(typeface.GlyphTypeface));

                var splitResult = buffer.Split(1);

                Assert.NotNull(splitResult.First);
                Assert.Equal(1, splitResult.First.Length);

                buffer = splitResult.Second;

                Assert.NotNull(buffer);

                //\"๊  
                splitResult = buffer.Split(1);

                Assert.NotNull(splitResult.First);
                Assert.Equal(2, splitResult.First.Length);

                buffer = splitResult.Second;

                Assert.NotNull(buffer);
            }
        }

        [Fact]
        public void Should_Split_RightToLeft()
        {
            var text = "أَبْجَدِيَّة عَرَبِيَّة";

            using (Start())
            {
                var codePoint = Codepoint.ReadAt(text, 0, out _);

                Assert.True(FontManager.Current.TryMatchCharacter(codePoint, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var typeface));
             
                var buffer = TextShaper.Current.ShapeText(text, new TextShaperOptions(typeface.GlyphTypeface));

                var splitResult = buffer.Split(6);

                var first = splitResult.First;

                Assert.NotNull(first);
                Assert.Equal(6, first.Length);
            }
        }

        [Fact]
        public void Should_Split_Zero_Length()
        {
            var text = "ABC";

            using (Start())
            {
                var buffer = TextShaper.Current.ShapeText(text, new TextShaperOptions(Typeface.Default.GlyphTypeface));

                var splitResult = buffer.Split(0);

                Assert.NotNull(splitResult.First);
                Assert.Equal(0, splitResult.First.Length);

                Assert.NotNull(splitResult.Second);

                Assert.Equal(text.Length, splitResult.Second.Length);
            }
        }

        [Fact]
        public void ClusterCache_SimpleMode_For_Latin_Text()
        {
            using (Start())
            {
                var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

                Assert.True(buffer.IsClusterCacheSimple, "Single-codepoint LTR text should use the simple cluster-cache mode.");
            }
        }

        [Fact]
        public void ClusterCache_SimpleMode_Measures_Correctly()
        {
            using (Start())
            {
                var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

                Assert.True(buffer.IsClusterCacheSimple);

                // Sum advances linearly and compare to TotalGlyphAdvance.
                var expectedTotal = 0d;
                for (var i = 0; i < buffer.Length; i++)
                {
                    expectedTotal += buffer[i].GlyphAdvance;
                }

                Assert.Equal(expectedTotal, buffer.TotalGlyphAdvance, 5);

                // Measure: ask for the width of the first 3 glyphs.
                var threeGlyphsWidth = buffer[0].GlyphAdvance + buffer[1].GlyphAdvance + buffer[2].GlyphAdvance;
                var fit = buffer.MeasureCharactersThatFit(threeGlyphsWidth, out var widthConsumed);

                Assert.Equal(3, fit);
                Assert.Equal(threeGlyphsWidth, widthConsumed, 5);

                // FirstClusterCharLength must be 1 in simple mode.
                Assert.Equal(1, buffer.FirstClusterCharLength);
            }
        }

        [Fact]
        public void ClusterCache_SimpleMode_Survives_Split()
        {
            using (Start())
            {
                var buffer = TextShaper.Current.ShapeText("ABCDEFGH", new TextShaperOptions(Typeface.Default.GlyphTypeface));

                Assert.True(buffer.IsClusterCacheSimple);

                var split = buffer.Split(3);

                Assert.NotNull(split.First);
                Assert.NotNull(split.Second);
                Assert.Equal(3, split.First!.Length);
                Assert.Equal(5, split.Second!.Length);

                Assert.True(split.First.IsClusterCacheSimple, "Split halves of a simple-mode buffer should also be simple-mode.");
                Assert.True(split.Second.IsClusterCacheSimple);

                var firstWidth = buffer[0].GlyphAdvance + buffer[1].GlyphAdvance + buffer[2].GlyphAdvance;
                Assert.Equal(firstWidth, split.First.TotalGlyphAdvance, 5);
            }
        }

        [Fact]
        public void ClusterCache_NotSimpleMode_For_ComplexClusters()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Cascadia Code"));

                // Same text the existing Should_Not_Split_Cluster test uses: contains a
                // two-codepoint cluster that breaks the one-char-per-cluster invariant.
                var buffer = TextShaper.Current.ShapeText("a\"๊a", new TextShaperOptions(typeface.GlyphTypeface));

                Assert.False(buffer.IsClusterCacheSimple,
                    "Multi-char clusters should fall back to the full cluster-start-chars table.");
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
