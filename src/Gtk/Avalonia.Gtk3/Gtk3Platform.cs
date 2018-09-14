using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Gtk3;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Gtk3
{
    public class Gtk3Platform : IWindowingPlatform, IPlatformSettings, IPlatformThreadingInterface
    {
        internal static readonly Gtk3Platform Instance = new Gtk3Platform();
        internal static readonly MouseDevice Mouse = new MouseDevice();
        internal static readonly KeyboardDevice Keyboard = new KeyboardDevice();
        internal static IntPtr App { get; set; }
        internal static string DisplayClassName;
        public static bool UseDeferredRendering = true;
        private static bool s_gtkInitialized;
        public static void Initialize()
        {
            if (!s_gtkInitialized)
            {
                try
                {
                    X11.XInitThreads();
                }catch{}
                Resolver.Resolve();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    using (var backends = new Utf8Buffer("x11"))
                        Native.GdkSetAllowedBackends?.Invoke(backends);
                Native.GtkInit(0, IntPtr.Zero);
                var disp = Native.GdkGetDefaultDisplay();
                DisplayClassName =
                    Utf8Buffer.StringFromPtr(Native.GTypeName(Marshal.ReadIntPtr(Marshal.ReadIntPtr(disp))));

                using (var utf = new Utf8Buffer("avalonia.app." + Guid.NewGuid()))
                    App = Native.GtkApplicationNew(utf, 0);
                //Mark current thread as UI thread
                s_tlsMarker = true;
                s_gtkInitialized = true;
            }
            AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>().ToConstant(Instance)
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory())
                .Bind<IKeyboardDevice>().ToConstant(Keyboard)
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(Instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialog>()
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoader());

        }

        public IWindowImpl CreateWindow() => new WindowImpl();

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup() => new PopupImpl();

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(100); //STUB
        public double RenderScalingFactor { get; } = 1;
        public double LayoutScalingFactor { get; } = 1;

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
                Native.GtkMainIteration();
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            var msec = interval.TotalMilliseconds;
            var imsec = (uint) msec;
            if (imsec == 0)
                imsec = 1;
            return GlibTimeout.StartTimer(GlibPriority.FromDispatcherPriority(priority), imsec, tick);
        }

        private bool[] _signaled = new bool[(int) DispatcherPriority.MaxValue + 1];
        object _lock = new object();
        public void Signal(DispatcherPriority prio)
        {
            var idx = (int) prio;
            lock(_lock)
                if (!_signaled[idx])
                {
                    _signaled[idx] = true;
                    GlibTimeout.Add(GlibPriority.FromDispatcherPriority(prio), 0, () =>
                    {
                        lock (_lock)
                        {
                            _signaled[idx] = false;
                        }
                        Signaled?.Invoke(prio);
                        return false;
                    });
                }
        }
        public event Action<DispatcherPriority?> Signaled;


        [ThreadStatic]
        private static bool s_tlsMarker;

        public bool CurrentThreadIsLoopThread => s_tlsMarker;
    }
}

namespace Avalonia
{
    public static class Gtk3AppBuilderExtensions
    {
        public static T UseGtk3<T>(this AppBuilderBase<T> builder, bool deferredRendering = true, ICustomGtk3NativeLibraryResolver resolver = null) 
            where T : AppBuilderBase<T>, new()
        {
            Resolver.Custom = resolver;
            Gtk3Platform.UseDeferredRendering = deferredRendering;
            return builder.UseWindowingSubsystem(Gtk3Platform.Initialize, "GTK3");
        }
    }
}
