using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Web.Blazor
{
    public class BlazorWindowingPlatform : IWindowingPlatform, IPlatformSettings, IPlatformThreadingInterface
    {
        private static object _uiLock = new object();
        private static object _syncRootLock = new object();
        private bool _signaled = false;
        private static int _uiThreadId = -1;
        private static int _lockNesting = 0;

        public IWindowImpl CreateWindow() => throw new NotSupportedException();

        IWindowImpl IWindowingPlatform.CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            return null;
        }

        public static KeyboardDevice Keyboard { get; private set; }

        public static void Register()
        {
            var instance = new BlazorWindowingPlatform();
            Keyboard = new KeyboardDevice();
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToSingleton<ClipboardStub>()
                .Bind<ICursorFactory>().ToSingleton<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(Keyboard)
                .Bind<IPlatformSettings>().ToConstant(instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(instance)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(ManualTriggerRenderTimer.Instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogsStub>()
                .Bind<IWindowingPlatform>().ToConstant(instance)
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoaderStub>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
        }

        public Size DoubleClickSize { get; } = new Size(2, 2);

        public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);

        public void RunLoop(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return AvaloniaLocator.Current.GetService<IRuntimePlatform>()
                .StartSystemTimer(interval, () =>
                {
                    using (Lock())
                    {
                        Dispatcher.UIThread.RunJobs(priority);
                        tick();
                    }
                });
        }

        public void Signal(DispatcherPriority priority)
        {
            lock (_syncRootLock)
            {
                if (_signaled)
                    return;
                _signaled = true;
                IDisposable disp = null;
                disp = AvaloniaLocator.Current.GetService<IRuntimePlatform>()
                    .StartSystemTimer(TimeSpan.FromMilliseconds(1),
                        () =>
                        {
                            lock (_syncRootLock)
                            {
                                _signaled = false;
                                disp.Dispose();
                            }

                            using (Lock())
                                Signaled?.Invoke(null);
                        });
            }
        }

        public bool CurrentThreadIsLoopThread
        {
            get
            {
                return true; // Blazor is single threaded.
            }
        }

        public event Action<DispatcherPriority?> Signaled;

        class LockDisposable : IDisposable
        {
            public void Dispose()
            {
                lock (_syncRootLock)
                {
                    _lockNesting--;
                    if (_lockNesting == 0)
                        _uiThreadId = -1;
                }

                Monitor.Exit(_uiLock);
            }
        }

        public static IDisposable Lock()
        {
            Monitor.Enter(_uiLock);
            lock (_syncRootLock)
            {
                _lockNesting++;
                _uiThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            return new LockDisposable();
        }
    }
}
