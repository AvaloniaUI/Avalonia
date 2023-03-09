using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class FontFamilyLoaderTests : IDisposable
    {
        private const string AssetMyFontRegular = AssetLocation + ".MyFont Regular.ttf" + Assembly + FontName;
        private const string FontName = "#MyFont";
        private const string Assembly = "?assembly=Avalonia.Visuals.UnitTests";
        private const string AssetLocation = "resm:Avalonia.Visuals.UnitTests.Assets";
        private const string AssetLocationAvares = "avares://Avalonia.Visuals.UnitTests";
        private const string AssetYourFileName = "/Assets/YourFont.ttf";
        private const string AssetYourFontAvares = AssetLocationAvares + AssetYourFileName;

        private readonly IDisposable _testApplication;

        public FontFamilyLoaderTests()
        {
            const string AssetMyFontBold = AssetLocation + ".MyFont Bold.ttf" + Assembly + FontName;
            const string AssetYourFont = AssetLocation + ".YourFont.ttf" + Assembly + FontName;

            var fontAssets = new[]
            {
                (AssetMyFontRegular, "AssetData"),
                (AssetMyFontBold, "AssetData"),
                (AssetYourFont, "AssetData"),
                (AssetYourFontAvares, "AssetData")
            };

            _testApplication = StartWithResources(fontAssets);
        }

        public void Dispose()
        {
            _testApplication.Dispose();
        }

        [Fact]
        public void Should_Load_Single_FontAsset()
        {
            var source = new Uri(AssetMyFontRegular, UriKind.RelativeOrAbsolute);

            var fontAssets = FontFamilyLoader.LoadFontAssets(source);

            Assert.Single(fontAssets);
        }

        [Fact]
        public void Should_Load_Single_FontAsset_Avares_Without_BaseUri()
        {
            var source = new Uri(AssetYourFontAvares);

            var fontAssets = FontFamilyLoader.LoadFontAssets(source);

            Assert.Single(fontAssets);
        }

        [Fact]
        public void Should_Load_Single_FontAsset_Avares_With_BaseUri()
        {
            var source = new Uri(AssetYourFileName, UriKind.RelativeOrAbsolute);
            var baseUri = new Uri(AssetLocationAvares);

            var fontAssets = FontFamilyLoader.LoadFontAssets(new Uri(baseUri, source));

            Assert.Single(fontAssets);
        }

        [Fact]
        public void Should_Load_Matching_Assets()
        {
            var source = new Uri(AssetLocation + ".MyFont*.ttf" + Assembly + FontName, UriKind.RelativeOrAbsolute);

            var fontAssets = FontFamilyLoader.LoadFontAssets(source).ToArray();

            foreach (var fontAsset in fontAssets)
            {
                Debug.WriteLine(fontAsset);
            }

            Assert.Equal(2, fontAssets.Length);
        }

        [Fact]
        public void Should_Load_Embedded_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                var source = new Uri("resm:Avalonia.Base.UnitTests.Assets?assembly=Avalonia.Base.UnitTests#Noto Mono", UriKind.RelativeOrAbsolute);

                var fontAssets = FontFamilyLoader.LoadFontAssets(source).ToArray();

                Assert.NotEmpty(fontAssets);

                foreach (var fontAsset in fontAssets)
                {
                    var stream = assetLoader.Open(fontAsset);

                    Assert.NotNull(stream);
                }
            }
        }

        private static IDisposable StartWithResources(params (string, string)[] assets)
        {
            var assetLoader = new MockAssetLoader(assets);
            var services = new TestServices(assetLoader: assetLoader, platform: new StandardRuntimePlatform());
            return UnitTestApplication.Start(services);
        }
    }
}
