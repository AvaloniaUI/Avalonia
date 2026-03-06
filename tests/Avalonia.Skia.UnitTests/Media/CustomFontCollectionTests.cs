using System;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class CustomFontCollectionTests
    {
        private const string AssetsNamespace = "Avalonia.Skia.UnitTests.Assets";
        private const string AssetFonts = $"resm:{AssetsNamespace}?assembly=Avalonia.Skia.UnitTests";

        [Fact]
        public void Should_AddGlyphTypeface_By_Stream()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;

                var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));

                fontManager.AddFontCollection(fontCollection);

                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                var infos = new[]
                {
                    new FontAssetInfo($"{AssetsNamespace}.AdobeBlank2VF.ttf", "Adobe Blank 2 VF R"),
                    new FontAssetInfo($"{AssetsNamespace}.Inter-Regular.ttf", "Inter"),
                    new FontAssetInfo($"{AssetsNamespace}.Manrope-Light.ttf", "Manrope Light"),
                    new FontAssetInfo($"{AssetsNamespace}.MiSans-Normal.ttf", "MiSans Normal"),
                    new FontAssetInfo($"{AssetsNamespace}.NISC18030.ttf", "GB18030 Bitmap"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoMono-Regular.ttf", "Noto Mono"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoSans-Italic.ttf", "Noto Sans"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoSansArabic-Regular.ttf", "Noto Sans Arabic"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoSansDeseret-Regular.ttf", "Noto Sans Deseret"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoSansHebrew-Regular.ttf", "Noto Sans Hebrew"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoSansMiao-Regular.ttf", "Noto Sans Miao"),
                    new FontAssetInfo($"{AssetsNamespace}.NotoSansTamil-Regular.ttf", "Noto Sans Tamil"),
                    new FontAssetInfo($"{AssetsNamespace}.SourceSerif4_36pt-Italic.ttf", "Source Serif 4 36pt"),
                    new FontAssetInfo($"{AssetsNamespace}.TwitterColorEmoji-SVGinOT.ttf", "Twitter Color Emoji")
                };

                var assets = assetLoader.GetAssets(new Uri(AssetFonts, UriKind.Absolute), null)
                    .OrderBy(uri => uri.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Assert.Equal(infos.Length, assets.Length);

                for (var i = 0; i < infos.Length; ++i)
                {
                    var info = infos[i];
                    var asset = assets[i];

                    Assert.Equal(info.Path, asset.AbsolutePath);

                    using var fontStream = assetLoader.Open(asset);
                    Assert.NotNull(fontStream);

                    Assert.True(fontCollection.TryAddGlyphTypeface(fontStream, out var glyphTypeface));
                    Assert.Equal(info.FamilyName, glyphTypeface.FamilyName);

                    Assert.True(fontManager.TryGetGlyphTypeface(new Typeface($"fonts:custom#{info.FamilyName}"), out var secondGlyphTypeface));
                    Assert.Same(glyphTypeface, secondGlyphTypeface);
                }
            }
        }

        [Fact]
        public void Should_Enumerate_FontFamilies()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;

                var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));

                fontManager.AddFontCollection(fontCollection);

                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                var assets = assetLoader.GetAssets(new Uri(AssetFonts, UriKind.Absolute), null).Where(x => x.AbsolutePath.EndsWith(".ttf")).ToArray();

                foreach (var asset in assets)
                {
                    fontCollection.TryAddGlyphTypeface(assetLoader.Open(asset), out _);
                }

                var families = fontCollection.ToArray();

                Assert.True(families.Length >= assets.Length);

                var other = new CustomFontCollection(new Uri("fonts:other", UriKind.Absolute));

                foreach (var family in families)
                {
                    var familyTypefaces = family.FamilyTypefaces;

                    foreach (var typeface in familyTypefaces)
                    {
                        other.TryAddGlyphTypeface(typeface.GlyphTypeface);
                    }
                }

                Assert.Equal(families.Length, other.Count);

                for (int i = 0; i < families.Length; i++)
                {
                    Assert.Equal(families[i].Name, other[i].Name);
                }
            }
        }

        [Fact]
        public void Should_AddFontSource_From_File()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;
                var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));
                fontManager.AddFontCollection(fontCollection);

                // Path to the test font
                var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");
                Assert.True(File.Exists(fontPath));

                var fontUri = new Uri(fontPath, UriKind.Absolute);

                // Add the font file
                Assert.True(fontCollection.TryAddFontSource(fontUri));

                // Check if the font was loaded
                Assert.True(fontCollection.TryGetGlyphTypeface("Inter", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var glyphTypeface));
                Assert.Equal("Inter", glyphTypeface.FamilyName);

                // Check if the FontManager can find the font
                Assert.True(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Inter"), out var glyphTypeface2));
                Assert.Equal(glyphTypeface, glyphTypeface2);
            }
        }

        [Fact]
        public void Should_AddFontSource_From_Folder()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;
                var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));
                fontManager.AddFontCollection(fontCollection);

                // Path to the test fonts
                var fontsFolder = Path.Combine(AppContext.BaseDirectory, "Assets");
                Assert.True(Directory.Exists(fontsFolder));

                var folderUri = new Uri(fontsFolder + Path.DirectorySeparatorChar, UriKind.Absolute);

                // Add the fonts
                Assert.True(fontCollection.TryAddFontSource(folderUri));

                // Check if the font was loaded
                Assert.True(fontCollection.TryGetGlyphTypeface("Inter", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var glyphTypeface));
                Assert.Equal("Inter", glyphTypeface.FamilyName);

                // Check if the FontManager can find the font
                Assert.True(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Inter"), out var glyphTypeface2));
                Assert.Equal(glyphTypeface, glyphTypeface2);
            }
        }

        [Fact]
        public void Should_AddFontSource_From_Resource()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;
                var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));
                fontManager.AddFontCollection(fontCollection);

                var allFontsUri = new Uri(AssetFonts, UriKind.Absolute);

                // Add the font resource
                Assert.True(fontCollection.TryAddFontSource(allFontsUri));

                // Get the loaded family names
                var families = fontCollection.ToArray();

                Assert.NotEmpty(families);

                // Try to get a GlyphTypeface
                Assert.True(fontCollection.TryGetGlyphTypeface("Noto Mono", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var glyphTypeface));
                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);

                // Check if the FontManager can find the font
                Assert.True(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Noto Mono"), out var glyphTypeface2));
                Assert.Equal(glyphTypeface, glyphTypeface2);
            }
        }



        private class CustomFontCollection(Uri key) : FontCollectionBase
        {
            public override Uri Key { get; } = key;
        }

        private record struct FontAssetInfo(string Path, string FamilyName);
    }
}
