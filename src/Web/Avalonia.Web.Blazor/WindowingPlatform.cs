using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

#nullable enable

namespace Avalonia.Web.Blazor
{
    public class BlazorWindowingPlatform : IWindowingPlatform, IPlatformSettings, IPlatformThreadingInterface
    {
        private bool _signaled;
        private static int s_uiThreadId = -1;

        public IWindowImpl CreateWindow() => throw new NotSupportedException();

        IWindowImpl IWindowingPlatform.CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public ITrayIconImpl? CreateTrayIcon()
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
            
            disp = AvaloniaLocator.Current.GetService<IRuntimePlatform>()
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
                return true; // Blazor is single threaded.
            }
        }

        public event Action<DispatcherPriority?>? Signaled;

        
    }
}
