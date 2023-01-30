﻿using System;
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
                var text = "\n\r\n";
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12,0, CultureInfo.CurrentCulture);
                var shapedBuffer = TextShaper.Current.ShapeText(text, options);
                
                Assert.Equal(shapedBuffer.Length, text.Length);
                Assert.Equal(shapedBuffer.GlyphInfos.Length, text.Length);
                Assert.Equal(0, shapedBuffer.GlyphInfos[0].GlyphCluster);
                Assert.Equal(1, shapedBuffer.GlyphInfos[1].GlyphCluster);
                Assert.Equal(1, shapedBuffer.GlyphInfos[2].GlyphCluster);
            }
        }

        [Fact]
        public void Should_Apply_IncrementalTabWidth()
        {
            using (Start())
            {
                var text = "\t";
                var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture, 100);
                var shapedBuffer = TextShaper.Current.ShapeText(text, options);

                Assert.Equal(shapedBuffer.Length, text.Length);
                Assert.Equal(100, shapedBuffer.GlyphInfos[0].GlyphAdvance);
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
