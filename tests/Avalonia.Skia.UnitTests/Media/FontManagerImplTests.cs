using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontManagerImplTests
    {
        private static string s_fontUri = "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono";

        [Fact]
        public void Should_Create_Typeface_From_Fallback()
        {
            var fontManager = new FontManagerImpl();

            var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                new Typeface(new FontFamily("A, B, " + fontManager.GetDefaultFontFamilyName())));

            var skTypeface = glyphTypeface.Typeface;

            Assert.Equal(SKTypeface.Default.FamilyName, skTypeface.FamilyName);

            Assert.Equal(SKTypeface.Default.FontWeight, skTypeface.FontWeight);

            Assert.Equal(SKTypeface.Default.FontSlant, skTypeface.FontSlant);
        }

        [Fact]
        public void Should_Create_Typeface_From_Fallback_Bold()
        {
            var fontManager = new FontManagerImpl();

            var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                new Typeface(new FontFamily($"A, B, Arial"), weight: FontWeight.Bold));

            var skTypeface = glyphTypeface.Typeface;
            
            Assert.True(skTypeface.FontWeight >= 600);
        }

        [Fact]
        public void Should_Create_Typeface_For_Unknown_Font()
        {
            var fontManager = new FontManagerImpl();

            var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                new Typeface(new FontFamily("Unknown")));

            var skTypeface = glyphTypeface.Typeface;

            Assert.Equal(SKTypeface.Default.FamilyName, skTypeface.FamilyName);

            Assert.Equal(SKTypeface.Default.FontWeight, skTypeface.FontWeight);

            Assert.Equal(SKTypeface.Default.FontSlant, skTypeface.FontSlant);
        }

        [Fact]
        public void Should_Load_Typeface_From_Resource()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(s_fontUri));

                var skTypeface = glyphTypeface.Typeface;

                Assert.Equal("Noto Mono", skTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Nearest_Matching_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(s_fontUri, FontStyle.Italic, FontWeight.Black));

                var skTypeface = glyphTypeface.Typeface;

                Assert.Equal("Noto Mono", skTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Throw_For_Invalid_Custom_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontManager = new FontManagerImpl();

                Assert.Throws<InvalidOperationException>(() =>
                    fontManager.CreateGlyphTypeface(
                        new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Unknown")));
            }
        }
    }
}
