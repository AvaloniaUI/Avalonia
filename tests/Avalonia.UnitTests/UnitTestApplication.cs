// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using Avalonia.Input.Platform;
using Avalonia.Animation;

namespace Avalonia.UnitTests
{
    public class UnitTestApplication : Application
    {
        private readonly TestServices _services;

        public UnitTestApplication() : this(null)
        {
            
        }
        
        public UnitTestApplication(TestServices services)
        {
            _services = services ?? new TestServices();
            RegisterServices();
        }

        public static new UnitTestApplication Current => (UnitTestApplication)Application.Current;

        public TestServices Services => _services;

        public static IDisposable Start(TestServices services = null)
        {
            var scope = AvaloniaLocator.EnterScope();
            var app = new UnitTestApplication(services);
            AvaloniaLocator.CurrentMutable.BindToSelf<Application>(app);
            Dispatcher.UIThread.UpdateServices();
            return Disposable.Create(() =>
            {
                scope.Dispose();
                Dispatcher.UIThread.UpdateServices();
            });
        }

        public override void RegisterServices()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(Services.AssetLoader)
                .Bind<IGlobalClock>().ToConstant(Services.GlobalClock)
                .BindToSelf<IGlobalStyles>(this)
                .Bind<IInputManager>().ToConstant(Services.InputManager)
                .Bind<IKeyboardNavigationHandler>().ToConstant(Services.KeyboardNavigation)
                .Bind<IMouseDevice>().ToConstant(Services.MouseDevice?.Invoke())
                .Bind<IRuntimePlatform>().ToConstant(Services.Platform)
                .Bind<IPlatformRenderInterface>().ToConstant(Services.RenderInterface)
                .Bind<IPlatformThreadingInterface>().ToConstant(Services.ThreadingInterface)
                .Bind<IScheduler>().ToConstant(Services.Scheduler)
                .Bind<IStandardCursorFactory>().ToConstant(Services.StandardCursorFactory)
                .Bind<IStyler>().ToConstant(Services.Styler)
                .Bind<IWindowingPlatform>().ToConstant(Services.WindowingPlatform)
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IApplicationLifecycle>().ToConstant(this);
            var styles = Services.Theme?.Invoke();

            if (styles != null)
            {
                Styles.AddRange(styles);
            }
        }
    }
}
