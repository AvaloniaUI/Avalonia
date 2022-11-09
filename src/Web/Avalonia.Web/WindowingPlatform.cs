using System;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Web
{
    internal class BrowserWindowingPlatform : IWindowingPlatform, IPlatformThreadingInterface
    {
        private bool _signaled;
        private static KeyboardDevice? s_keyboard;

        public IWindowImpl CreateWindow() => throw new NotSupportedException();

        IWindowImpl IWindowingPlatform.CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
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
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<ICursorFactory>().ToSingleton<CssCursorFactory>()
                .Bind<IKeyboardDevice>().ToConstant(s_keyboard)
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(instance)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(ManualTriggerRenderTimer.Instance)
                .Bind<IWindowingPlatform>().ToConstant(instance)
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoaderStub>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return GetRuntimePlatform()
                .StartSystemTimer(interval, () =>
                {
                    Dispatcher.UIThread.RunJobs(priority);
                    tick();
                });
        }

        public void Signal(DispatcherPriority priority)
        {
            if (_signaled)
                return;
            
            _signaled = true;
            
            IDisposable? disp = null;
            
            disp = GetRuntimePlatform()
                .StartSystemTimer(TimeSpan.FromMilliseconds(1),
                    () =>
                    {
                        _signaled = false;
                        disp?.Dispose();

                        Signaled?.Invoke(null);
                    });
        }

        public bool CurrentThreadIsLoopThread
        {
            get
            {
                return true; // Browser is single threaded.
            }
        }

        public event Action<DispatcherPriority?>? Signaled;

        private static IRuntimePlatform GetRuntimePlatform()
        {
            return AvaloniaLocator.Current.GetRequiredService<IRuntimePlatform>();
        }
    }
}
