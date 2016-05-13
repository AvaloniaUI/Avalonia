// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Controls;

namespace Avalonia.UnitTests
{
    public class UnitTestApplication : Application
    {
        public UnitTestApplication(TestServices services)
        {
            Services = services ?? new TestServices();
            RegisterServices();

            var styles = Services.Theme?.Invoke();

            if (styles != null)
            {
                Styles.AddRange(styles);
            }
        }

        public static new UnitTestApplication Current => (UnitTestApplication)Application.Current;

        public TestServices Services { get; }

        public static IDisposable Start(TestServices services = null)
        {
            var scope = AvaloniaLocator.EnterScope();
            var app = new UnitTestApplication(services);
            AvaloniaLocator.CurrentMutable.BindToSelf<Application>(app);
            return scope;
        }

        protected override void RegisterServices()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(Services.AssetLoader)
                .Bind<IFocusManager>().ToConstant(Services.FocusManager)
                .BindToSelf<IGlobalStyles>(this)
                .Bind<IInputManager>().ToConstant(Services.InputManager)
                .Bind<IKeyboardDevice>().ToConstant(Services.KeyboardDevice?.Invoke())
                .Bind<ILayoutManager>().ToConstant(Services.LayoutManager)
                .Bind<IPclPlatformWrapper>().ToConstant(Services.PlatformWrapper)
                .Bind<IPlatformRenderInterface>().ToConstant(Services.RenderInterface)
                .Bind<IPlatformThreadingInterface>().ToConstant(Services.ThreadingInterface)
                .Bind<IStandardCursorFactory>().ToConstant(Services.StandardCursorFactory)
                .Bind<IStyler>().ToConstant(Services.Styler)
                .Bind<IWindowingPlatform>().ToConstant(Services.WindowingPlatform)
                .Bind<IApplicationLifecycle>().ToConstant(this);
        }
    }
}
