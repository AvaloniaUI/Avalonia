// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media.Fonts
{
    public class FontFamilyLoaderTests
    {
        private const string fontName = "#MyFont";
        private const string assembly = "?assembly=Avalonia.Visuals.UnitTests";
        private const string assetLocation = "resm:Avalonia.Visuals.UnitTests.Assets";

        [Fact]
        public void Should_Load_Single_FontAsset()
        {
            const string fontAsset = assetLocation + ".MyFont-Regular.ttf" + assembly + fontName;

            using (StartWithResources((fontAsset, "AssetData")))
            {
                var source = new Uri(fontAsset, UriKind.RelativeOrAbsolute);

                var key = new FontFamilyKey(source);

                var fontAssets = FontFamilyLoader.LoadFontAssets(key);

                Assert.Single(fontAssets);
            }
        }

        [Fact]
        public void Should_Load_Matching_Assets()
        {
            const string assetMyFontRegular = assetLocation + ".MyFont-Regular.ttf" + assembly + fontName;
            const string assetMyFontBold = assetLocation + ".MyFont-Bold.ttf" + assembly + fontName;
            const string assetYourFont = assetLocation + ".YourFont.ttf" + assembly + fontName;

            var fontLocations = new[]
            {
                (assetMyFontRegular, "AssetData"),
                (assetMyFontBold, "AssetData"),
                (assetYourFont, "AssetData")
            };

            using (StartWithResources(fontLocations))
            {
                var source = new Uri(assetLocation + ".MyFont-*.ttf", UriKind.RelativeOrAbsolute);

                var key = new FontFamilyKey(source);

                var fontAssets = FontFamilyLoader.LoadFontAssets(key);

                Assert.Equal(2, fontAssets.Count());
            }
        }

        private static IDisposable StartWithResources(params (string, string)[] assets)
        {
            var assetLoader = new MockAssetLoader(assets);
            var services = new TestServices(assetLoader: assetLoader, platform: new AppBuilder().RuntimePlatform);
            return UnitTestApplication.Start(services);
        }
    }
}
