using System;
using System.Linq;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class FontManagerImplTests
    {
        private static string s_fontUri = "resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Mono";

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

            //we need to have a valid font name different from the default one
            string fontName = fontManager.GetInstalledFontFamilyNames().First();

            var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                new Typeface(new FontFamily($"A, B, {fontName}"), FontWeight.Bold));

            var skTypeface = glyphTypeface.Typeface;

            Assert.Equal(fontName, skTypeface.FamilyName);
            Assert.Equal(SKFontStyle.Bold.Weight, skTypeface.FontWeight);
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
            using (AvaloniaLocator.EnterScope())
            {
                var assetLoaderType = typeof(TestRoot).Assembly.GetType("Avalonia.Shared.PlatformSupport.AssetLoader");

                var assetLoader = (IAssetLoader)Activator.CreateInstance(assetLoaderType, (Assembly)null);

                AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>().ToConstant(assetLoader);

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(new FontFamily(s_fontUri)));

                var skTypeface = glyphTypeface.Typeface;

                Assert.Equal("Noto Mono", skTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Nearest_Matching_Font()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var assetLoaderType = typeof(TestRoot).Assembly.GetType("Avalonia.Shared.PlatformSupport.AssetLoader");

                var assetLoader = (IAssetLoader)Activator.CreateInstance(assetLoaderType, (Assembly)null);

                AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>().ToConstant(assetLoader);

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(new FontFamily(s_fontUri), FontWeight.Black, FontStyle.Italic));

                var skTypeface = glyphTypeface.Typeface;

                Assert.Equal("Noto Mono", skTypeface.FamilyName);
            }
        }
    }
}
