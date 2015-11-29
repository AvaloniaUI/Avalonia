// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Perspex.Controls.UnitTests;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using Perspex.Themes.Default;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace Perspex.LeakTests
{
    internal class TestApp : Application
    {
        private TestApp()
        {
            RegisterServices();

            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var windowImpl = new Mock<IWindowImpl>();
            var renderInterface = fixture.Create<IPlatformRenderInterface>();

            PerspexLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(new AssetLoader())
                .Bind<IPclPlatformWrapper>().ToConstant(new PclPlatformWrapper())
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface)
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock(() => windowImpl.Object));

            Styles = new DefaultTheme();
        }

        public static void Initialize()
        {
            if (Current == null)
            {
                new TestApp();
            }
        }
    }
}
