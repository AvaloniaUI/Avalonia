// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media.Fonts
{
    using System.Diagnostics;

    public class FontFamilyLoaderTests : IDisposable
    {
        private const string FontName = "#MyFont";
        private const string Assembly = "?assembly=Avalonia.Visuals.UnitTests";
        private const string AssetLocation = "resm:Avalonia.Visuals.UnitTests.Assets";

        private readonly IDisposable _testApplication;

        public FontFamilyLoaderTests()
        {
            const string AssetMyFontRegular = AssetLocation + ".MyFont-Regular.ttf" + Assembly + FontName;
            const string AssetMyFontBold = AssetLocation + ".MyFont-Bold.ttf" + Assembly + FontName;
            const string AssetYourFont = AssetLocation + ".YourFont.ttf" + Assembly + FontName;

            var fontAssets = new[]
                                    {
                                        (AssetMyFontRegular, "AssetData"),
                                        (AssetMyFontBold, "AssetData"),
                                        (AssetYourFont, "AssetData")
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
            const string FontAsset = AssetLocation + ".MyFont-Regular.ttf" + Assembly + FontName;

            var source = new Uri(FontAsset, UriKind.RelativeOrAbsolute);

            var key = new FontFamilyKey(source);

            var fontAssets = FontFamilyLoader.LoadFontAssets(key);

            Assert.Single(fontAssets);
        }

        [Fact]
        public void Should_Load_Matching_Assets()
        {
            var source = new Uri(AssetLocation + ".MyFont-*.ttf" + Assembly + FontName, UriKind.RelativeOrAbsolute);

            var key = new FontFamilyKey(source);

            var fontAssets = FontFamilyLoader.LoadFontAssets(key).ToArray();

            foreach (var fontAsset in fontAssets)
            {
                Debug.WriteLine(fontAsset);
            }

            Assert.Equal(2, fontAssets.Length);
        }

        private static IDisposable StartWithResources(params (string, string)[] assets)
        {
            var assetLoader = new MockAssetLoader(assets);
            var services = new TestServices(assetLoader: assetLoader, platform: new AppBuilder().RuntimePlatform);
            return UnitTestApplication.Start(services);
        }
    }
}
