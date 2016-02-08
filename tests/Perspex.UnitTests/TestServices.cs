// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using Perspex.Styling;
using Perspex.Themes.Default;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace Perspex.UnitTests
{
    public class TestServices
    {
        private static IFixture s_fixture = new Fixture().Customize(new AutoMoqCustomization());

        public static readonly TestServices StyledWindow = new TestServices
        {
            AssetLoader = new AssetLoader(),
            LayoutManager = new LayoutManager(),
            PlatformWrapper = new PclPlatformWrapper(),
            RenderInterface = s_fixture.Create<IPlatformRenderInterface>(),
            StandardCursorFactory = Mock.Of<IStandardCursorFactory>(),
            Styler = new Styler(),
            Theme = () => new DefaultTheme(),
            ThreadingInterface = Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true),
            WindowingPlatform = new MockWindowingPlatform(),
        };

        public IAssetLoader AssetLoader { get; set; }
        public ILayoutManager LayoutManager { get; set; }
        public IPclPlatformWrapper PlatformWrapper { get; set; }
        public IPlatformRenderInterface RenderInterface { get; set; }
        public IStandardCursorFactory StandardCursorFactory { get; set; }
        public IStyler Styler { get; set; }
        public Func<Styles> Theme { get; set; }
        public IPlatformThreadingInterface ThreadingInterface { get; set; }
        public IWindowImpl WindowImpl { get; set; }
        public IWindowingPlatform WindowingPlatform { get; set; }
    }
}
