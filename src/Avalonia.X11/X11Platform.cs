using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.FreeDesktop.DBusIme;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.X11;
using Avalonia.X11.Glx;
using Avalonia.X11.Screens;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    internal class AvaloniaX11Platform : IWindowingPlatform
    {
        private Lazy<KeyboardDevice> _keyboardDevice = new Lazy<KeyboardDevice>(() => new KeyboardDevice());
        public KeyboardDevice KeyboardDevice => _keyboardDevice.Value;
        public Dictionary<IntPtr, X11PlatformThreading.EventHandler> Windows =
            new Dictionary<IntPtr, X11PlatformThreading.EventHandler>();
        public XI2Manager XI2;
        public X11Info Info { get; private set; }
        public X11Screens X11Screens { get; private set; }
        public Compositor Compositor { get; private set; }
        public IScreenImpl Screens { get; private set; }
        public X11PlatformOptions Options { get; private set; }
        public IntPtr OrphanedWindow { get; private set; }
        public X11Globals Globals { get; private set; }
        public XResources Resources { get; private set; }
        public ManualRawEventGrouperDispatchQueue EventGrouperDispatchQueue { get; } = new();

        public void Initialize(X11PlatformOptions options)
        {
            Options = options;

            bool useXim = false;
            if (EnableIme(options))
            {
                // Attempt to configure DBus-based input method and check if we can fall back to XIM
                if (!X11DBusImeHelper.DetectAndRegister() && ShouldUseXim())
                    useXim = true;
            }

            XInitThreads();
            Display = XOpenDisplay(IntPtr.Zero);
            if (Display == IntPtr.Zero)
                throw new Exception("XOpenDisplay failed");
            DeferredDisplay = XOpenDisplay(IntPtr.Zero);
            if (DeferredDisplay == IntPtr.Zero)
                throw new Exception("XOpenDisplay failed");
                
            OrphanedWindow = XCreateSimpleWindow(Display, XDefaultRootWindow(Display), 0, 0, 1, 1, 0, IntPtr.Zero,
                IntPtr.Zero);
            XError.Init();

            Info = new X11Info(Display, DeferredDisplay, useXim);
            Globals = new X11Globals(this);
            Resources = new XResources(this);
            //TODO: log
            if (options.UseDBusMenu)
                DBusHelper.TryInitialize();

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IDispatcherImpl>().ToConstant(new X11PlatformThreading(this))
                .Bind<IRenderTimer>().ToConstant(new SleepLoopRenderTimer(60))
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Control))
                .Bind<IKeyboardDevice>().ToFunc(() => KeyboardDevice)
                .Bind<ICursorFactory>().ToConstant(new X11CursorFactory(Display))
                .Bind<IClipboard>().ToConstant(new X11Clipboard(this))
                .Bind<IPlatformSettings>().ToSingleton<DBusPlatformSettings>()
                .Bind<IPlatformIconLoader>().ToConstant(new X11IconLoader())
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new LinuxMountedVolumeInfoProvider())
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(new X11PlatformLifetimeEvents(this));
            
            Screens = X11Screens = new X11Screens(this);
            if (Info.XInputVersion != null)
            {
                var xi2 = new XI2Manager();
                if (xi2.Init(this))
                    XI2 = xi2;
            }

            var graphics = InitializeGraphics(options, Info);
            if (graphics is not null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(graphics);
            }

            Compositor = new Compositor(graphics);
            AvaloniaLocator.CurrentMutable.Bind<Compositor>().ToConstant(Compositor);
        }

        public IntPtr DeferredDisplay { get; set; }
        public IntPtr Display { get; set; }

        private static uint[] X11IconConverter(IWindowIconImpl icon)
        {
            if (!(icon is X11IconData x11icon))
                return Array.Empty<uint>();

            return x11icon.Data.Select(x => x.ToUInt32()).ToArray();
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            var dbusTrayIcon = new DBusTrayIconImpl();

            if (!dbusTrayIcon.IsActive) return new XEmbedTrayIconImpl();

            dbusTrayIcon.IconConverterDelegate = X11IconConverter;

            return dbusTrayIcon;
        }
        
        public IWindowImpl CreateWindow()
        {
            return new X11Window(this, null);
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }

        private static bool EnableIme(X11PlatformOptions options)
        {
            // Disable if explicitly asked by user
            var avaloniaImModule = Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE");
            if (avaloniaImModule == "none")
                return false;
            
            // Use value from options when specified
            if (options.EnableIme.HasValue)
                return options.EnableIme.Value;
            
            // Automatically enable for CJK locales
            var lang = Environment.GetEnvironmentVariable("LANG");
            var isCjkLocale = lang != null &&
                              (lang.Contains("zh")
                               || lang.Contains("ja")
                               || lang.Contains("vi")
                               || lang.Contains("ko"));

            return isCjkLocale;
        }

        private static bool ShouldUseXim()
        {
            // Check if we are forbidden from using IME
            if (Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE") == "none"
                || Environment.GetEnvironmentVariable("GTK_IM_MODULE") == "none"
                || Environment.GetEnvironmentVariable("QT_IM_MODULE") == "none")
                return true;
            
            // Check if XIM is configured
            var modifiers = Environment.GetEnvironmentVariable("XMODIFIERS");
            if (modifiers == null)
                return false;
            if (modifiers.Contains("@im=none") || modifiers.Contains("@im=null"))
                return false;
            if (!modifiers.Contains("@im="))
                return false;
            
            // Check if we are configured to use it
            if (Environment.GetEnvironmentVariable("GTK_IM_MODULE") == "xim"
                || Environment.GetEnvironmentVariable("QT_IM_MODULE") == "xim"
                || Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE") == "xim")
                return true;
            
            return false;
        }
        
        private static IPlatformGraphics InitializeGraphics(X11PlatformOptions opts, X11Info info)
        {
            if (opts.RenderingMode is null || !opts.RenderingMode.Any())
            {
                throw new InvalidOperationException($"{nameof(X11PlatformOptions)}.{nameof(X11PlatformOptions.RenderingMode)} must not be empty or null");
            }

            foreach (var renderingMode in opts.RenderingMode)
            {
                if (renderingMode == X11RenderingMode.Software)
                {
                    return null;
                }
                
                if (renderingMode == X11RenderingMode.Glx)
                {
                    if (GlxPlatformGraphics.TryCreate(info, opts.GlProfiles) is { } glx)
                    {
                        return glx;
                    }
                }

                if (renderingMode == X11RenderingMode.Egl)
                {
                    if (EglPlatformGraphics.TryCreate() is { } egl)
                    {
                        return egl;
                    }
                }
            }

            throw new InvalidOperationException($"{nameof(X11PlatformOptions)}.{nameof(X11PlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
        }
    }
}

namespace Avalonia
{
    /// <summary>
    /// Represents the rendering mode for platform graphics.
    /// </summary>
    public enum X11RenderingMode
    {
        /// <summary>
        /// Avalonia is rendered into a framebuffer.
        /// </summary>
        Software = 1,

        /// <summary>
        /// Enables Glx rendering.
        /// </summary>
        Glx = 2,

        /// <summary>
        /// Enables native Linux EGL rendering.
        /// </summary>
        Egl = 3
    }
    
    /// <summary>
    /// Platform-specific options which apply to Linux.
    /// </summary>
    public class X11PlatformOptions
    {
        /// <summary>
        /// Gets or sets Avalonia rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="X11RenderingMode.Glx"/>, <see cref="X11RenderingMode.Software"/>.
        /// </summary>
        /// <remarks>
        /// If application should work on as wide range of devices as possible, at least add <see cref="X11RenderingMode.Software"/> as a fallback value.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<X11RenderingMode> RenderingMode { get; set; } = new[]
        {
            X11RenderingMode.Glx, X11RenderingMode.Software
        };

        /// <summary>
        /// Embeds popups to the window when set to true. The default value is false.
        /// </summary>
        public bool OverlayPopups { get; set; }

        /// <summary>
        /// Enables global menu support on Linux desktop environments where it's supported (e. g. XFCE and MATE with plugin, KDE, etc).
        /// The default value is true.
        /// </summary>
        public bool UseDBusMenu { get; set; } = true;

        /// <summary>
        /// Enables DBus file picker instead of GTK.
        /// The default value is true.
        /// </summary>
        public bool UseDBusFilePicker { get; set; } = true;
        
        /// <summary>
        /// Determines whether to use IME.
        /// IME would be enabled by default if the current user input language is one of the following: Mandarin, Japanese, Vietnamese or Korean.
        /// </summary>
        /// <remarks>
        /// Input method editor is a component that enables users to generate characters not natively available 
        /// on their input devices by using sequences of characters or mouse operations that are natively available on their input devices.
        /// </remarks>
        public bool? EnableIme { get; set; } = true;

        /// <summary>
        /// Determines whether to use Input Focus Proxy.
        /// The default value is false.
        /// </summary> 
        public bool EnableInputFocusProxy { get; set; }
        
        /// <summary>
        /// Determines whether to enable support for the
        /// X Session Management Protocol.
        /// </summary>
        /// <remarks>
        /// X Session Management Protocol is a standard implemented on most
        /// Linux systems that uses Xorg. This enables apps to control how they
        /// can control and/or cancel the pending shutdown requested by the user.
        /// </remarks>
        public bool EnableSessionManagement { get; set; } = 
            Environment.GetEnvironmentVariable("AVALONIA_X11_USE_SESSION_MANAGEMENT") != "0";
        
        public IList<GlVersion> GlProfiles { get; set; } = new List<GlVersion>
        {
            new GlVersion(GlProfileType.OpenGL, 4, 0),
            new GlVersion(GlProfileType.OpenGL, 3, 2),
            new GlVersion(GlProfileType.OpenGL, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 3, 2),
            new GlVersion(GlProfileType.OpenGLES, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 2, 0)
        };

        public IList<string> GlxRendererBlacklist { get; set; } = new List<string>
        {
            // llvmpipe is a software GL rasterizer. If it's returned by glGetString,
            // that usually means that something in the system is horribly misconfigured
            // and sometimes attempts to use GLX might cause a segfault
            "llvmpipe"
        };

        
        public string WmClass { get; set; }

        /// <summary>
        /// Enables multitouch support. The default value is true.
        /// </summary>
        /// <remarks>
        /// Multitouch allows a surface (a touchpad or touchscreen) to recognize the presence of more than one point of contact with the surface at the same time.
        /// </remarks>
        public bool? EnableMultiTouch { get; set; } = true;

        public X11PlatformOptions()
        {
            try
            {
                WmClass = Assembly.GetEntryAssembly()?.GetName()?.Name;
            }
            catch
            {
                //
            }
        }
    }
    public static class AvaloniaX11PlatformExtensions
    {
        public static AppBuilder UseX11(this AppBuilder builder)
        {
            builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() =>
                new AvaloniaX11Platform().Initialize(AvaloniaLocator.Current.GetService<X11PlatformOptions>() ??
                                                     new X11PlatformOptions()));
            return builder;
        }

        public static void InitializeX11Platform(X11PlatformOptions options = null) =>
            new AvaloniaX11Platform().Initialize(options ?? new X11PlatformOptions());
    }

}
