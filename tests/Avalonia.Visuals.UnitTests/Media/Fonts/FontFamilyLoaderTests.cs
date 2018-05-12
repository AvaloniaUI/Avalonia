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
        [Fact]
        public void Should_Load_Single_FontResource()
        {
            const string resourcePath = "resm:Avalonia.Visuals.UnitTests.Assets.MyFont.ttf?assembly=Avalonia.Visuals.UnitTests#MyFont";

            using (StartWithResources((resourcePath, "MyFont.ttf")))
            {
                var source = new Uri(resourcePath, UriKind.RelativeOrAbsolute);

                var key = new FontFamilyKey(source);

                var resources = FontFamilyLoader.LoadFontResources(key);

                Assert.Single(resources);
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
