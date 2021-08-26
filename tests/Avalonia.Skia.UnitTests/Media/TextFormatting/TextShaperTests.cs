using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
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
                var text = "\n\r\n".AsMemory();

                var glyphRun = TextShaper.Current.ShapeText(
                    text,
                    Typeface.Default,
                    12,
                    CultureInfo.CurrentCulture);
                
                Assert.Equal(glyphRun.Characters.Length, text.Length);
                Assert.Equal(glyphRun.GlyphClusters.Length, text.Length);
                Assert.Equal(0, glyphRun.GlyphClusters[0]);
                Assert.Equal(1, glyphRun.GlyphClusters[1]);
                Assert.Equal(1, glyphRun.GlyphClusters[2]);
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
