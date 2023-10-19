using System;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Skia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Browser
{
    internal class BrowserWindowingPlatform : IWindowingPlatform, IPlatformThreadingInterface
    {
        private bool _signaled;
        private static KeyboardDevice? s_keyboard;

        public IWindowImpl CreateWindow() => throw new NotSupportedException("Browser doesn't support windowing platform. In order to display a single-view content, set ISingleViewApplicationLifetime.MainView.");

        IWindowImpl IWindowingPlatform.CreateEmbeddableWindow()
        {
            throw new NotImplementedException("Browser doesn't support embeddable windowing platform.");
        }

        public ITrayIconImpl? CreateTrayIcon()
        {
            return null;
        }

        public static KeyboardDevice Keyboard => s_keyboard ??
            throw new InvalidOperationException("BrowserWindowingPlatform not registered.");

        public static void Register()
        {
            var instance = new BrowserWindowingPlatform();

            s_keyboard = new KeyboardDevice();
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToSingleton<BrowserRuntimePlatform>()
                .Bind<ICursorFactory>().ToSingleton<CssCursorFactory>()
                .Bind<IKeyboardDevice>().ToConstant(s_keyboard)
                .Bind<IPlatformSettings>().ToSingleton<BrowserPlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(instance)
                .Bind<IRenderTimer>().ToConstant(ManualTriggerRenderTimer.Instance)
                .Bind<IWindowingPlatform>().ToConstant(instance)
                .Bind<IPlatformGraphics>().ToConstant(new BrowserSkiaGraphics())
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoaderStub>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

            if (AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() is { } options
                && options.RegisterAvaloniaServiceWorker)
            {
                var swPath = AvaloniaModule.ResolveServiceWorkerPath();
                AvaloniaModule.RegisterServiceWorker(swPath, options.AvaloniaServiceWorkerScope);
            }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            var timerId = CanvasHelper.SetTimeout(tick, (int)interval.TotalMilliseconds);
            return Reactive.Disposable.Create(() => CanvasHelper.ClearTimeout(timerId));
        }

        public void Signal(DispatcherPriority priority)
        {
            if (_signaled)
                return;

            _signaled = true;

            // NOTE: by HTML5 spec minimal timeout is 4ms, but Chrome seems to work well with 1ms as well.
            var interval = 1;

            CanvasHelper.SetTimeout(() =>
            {
                _signaled = false;
                Signaled?.Invoke(null);
            }, interval);
        }

        public bool CurrentThreadIsLoopThread
        {
            get
            {
                return true; // TODO: add MT support.
            }
        }

        public event Action<DispatcherPriority?>? Signaled;
    }
}
