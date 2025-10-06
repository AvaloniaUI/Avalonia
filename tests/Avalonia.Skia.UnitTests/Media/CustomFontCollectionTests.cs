using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Utilities;
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

                Assert.True(fontCollection.TryAddGlyphTypeface(notoMonoStream));

                Assert.True(fontCollection.TryGetGlyphTypeface("Inter", FontStyle.Normal, FontWeight.Regular, FontStretch.Normal, out var firstGlyphTypeface));

                Assert.Equal("Inter", firstGlyphTypeface.FamilyName);

                Assert.True(fontManager.TryGetGlyphTypeface(new Typeface("fonts:custom#Inter"), out var secondGlyphTypeface));

                Assert.Equal(firstGlyphTypeface, secondGlyphTypeface);
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
                    fontCollection.TryAddGlyphTypeface(assetLoader.Open(asset));
                }

                var families = fontCollection.ToArray();

                Assert.Equal(assets.Length, families.Length);
            }
        }
    }
}
