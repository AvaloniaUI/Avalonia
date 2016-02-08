// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Input;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Styling;

namespace Perspex.UnitTests
{
    public class UnitTestApplication : Application
    {
        public UnitTestApplication(TestServices services)
        {
            Services = services ?? new TestServices();
            RegisterServices();
            Styles = Services.Theme?.Invoke();
        }

        public static new UnitTestApplication Current => (UnitTestApplication)Application.Current;

        public TestServices Services { get; }

        public static IDisposable Start(TestServices services = null)
        {
            var scope = PerspexLocator.EnterScope();
            var app = new UnitTestApplication(services);
            return scope;
        }

        protected override void RegisterServices()
        {
            PerspexLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(Services.AssetLoader)
                .BindToSelf<IGlobalStyles>(this)
                .Bind<IInputManager>().ToConstant(Services.InputManager)
                .Bind<ILayoutManager>().ToConstant(Services.LayoutManager)
                .Bind<IPclPlatformWrapper>().ToConstant(Services.PlatformWrapper)
                .Bind<IPlatformRenderInterface>().ToConstant(Services.RenderInterface)
                .Bind<IPlatformThreadingInterface>().ToConstant(Services.ThreadingInterface)
                .Bind<IStandardCursorFactory>().ToConstant(Services.StandardCursorFactory)
                .Bind<IStyler>().ToConstant(Services.Styler)
                .Bind<IWindowingPlatform>().ToConstant(Services.WindowingPlatform);
        }
    }
}
