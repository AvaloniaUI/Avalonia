// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Perspex.Controls.UnitTests;
using Perspex.Layout;
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
            var windowImpl = Mock.Of<IWindowImpl>(x => x.Scaling == 1);
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            var threadingInterface = Mock.Of<IPlatformThreadingInterface>(x =>
                x.CurrentThreadIsLoopThread == true);

            PerspexLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(new AssetLoader())
                .Bind<ILayoutManager>().ToConstant(new LayoutManager())
                .Bind<IPclPlatformWrapper>().ToConstant(new PclPlatformWrapper())
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface)
                .Bind<IPlatformThreadingInterface>().ToConstant(threadingInterface)
                .Bind<IStandardCursorFactory>().ToConstant(new Mock<IStandardCursorFactory>().Object)
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock(() => windowImpl));

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
