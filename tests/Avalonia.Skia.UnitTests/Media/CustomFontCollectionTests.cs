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
        private const string NotoMono =
         "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests";

        [Fact]
        public void Should_AddGlyphTypeface_By_Stream()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;

                var fontCollection = new CustomFontCollection(new Uri("fonts:custom", UriKind.Absolute));

                fontManager.AddFontCollection(fontCollection);

                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                var assets = assetLoader.GetAssets(new Uri(NotoMono, UriKind.Absolute), null).ToArray();

                Assert.NotEmpty(assets);

                var notoMonoLocation = assets.First();

                using var notoMonoStream = assetLoader.Open(notoMonoLocation);

                Assert.NotNull(notoMonoStream);

                Assert.True(fontCollection.TryAddGlyphTypeface(notoMonoStream, out var glyphTypeface));

                Assert.Equal("Inter", glyphTypeface.FamilyName);

                Assert.True(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Inter"), out var secondGlyphTypeface));

                Assert.Equal(glyphTypeface, secondGlyphTypeface);
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

                var assets = assetLoader.GetAssets(new Uri(NotoMono, UriKind.Absolute), null).Where(x => x.AbsolutePath.EndsWith(".ttf")).ToArray();

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

                // Use the NotoMono resource as FontSource
                var notoMonoUri = new Uri(NotoMono, UriKind.Absolute);

                // Add the font resource
                Assert.True(fontCollection.TryAddFontSource(notoMonoUri));

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
    }
}
