using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Gtk3;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
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

        static bool EnvOption(string option, bool def, bool? specified)
        {
            bool? Parse(string env)
            {
                var v = Environment.GetEnvironmentVariable("AVALONIA_GTK3_" + env);
                if (v == null)
                    return null;
                if (v.ToLowerInvariant() == "false" || v == "0")
                    return false;
                return true;
            }

            var overridden = Parse(option + "_OVERRIDE");
            if (overridden.HasValue)
                return overridden.Value;
            if (specified.HasValue)
                return specified.Value;
            var envValue = Parse(option);
            return envValue ?? def;
        }
        
        public static void Initialize(Gtk3PlatformOptions options)
        {
            Resolver.Custom = options.CustomResolver;
            UseDeferredRendering = EnvOption("USE_DEFERRED_RENDERING", true, options.UseDeferredRendering);
            var useGpu = EnvOption("USE_GPU", true, options.UseGpuAcceleration);
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

                using (var utf = new Utf8Buffer($"avalonia.app.a{Guid.NewGuid().ToString("N")}"))
                    App = Native.GtkApplicationNew(utf, 0);
                //Mark current thread as UI thread
                s_tlsMarker = true;
                s_gtkInitialized = true;
            }
            AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>().ToConstant(Instance)
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory())
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(Instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialog>()
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoader());
            if (useGpu)
                EglGlPlatformFeature.TryInitialize();
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

    public class Gtk3PlatformOptions
    {
        public bool? UseDeferredRendering { get; set; }
        public bool? UseGpuAcceleration { get; set; }
        public ICustomGtk3NativeLibraryResolver CustomResolver { get; set; }
    }
}

namespace Avalonia
{
    public static class Gtk3AppBuilderExtensions
    {
        public static T UseGtk3<T>(this AppBuilderBase<T> builder, Gtk3PlatformOptions options = null) 
            where T : AppBuilderBase<T>, new()
        {
            return builder.UseWindowingSubsystem(() => Gtk3Platform.Initialize(options ?? new Gtk3PlatformOptions()),
                "GTK3");
        }
    }
}
