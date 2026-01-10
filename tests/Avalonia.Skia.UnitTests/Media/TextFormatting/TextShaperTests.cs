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

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
