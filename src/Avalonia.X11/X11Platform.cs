using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.X11;
using Avalonia.X11.Glx;
using Avalonia.X11.NativeDialogs;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    class AvaloniaX11Platform : IWindowingPlatform
    {
        private Lazy<KeyboardDevice> _keyboardDevice = new Lazy<KeyboardDevice>(() => new KeyboardDevice());
        private Lazy<MouseDevice> _mouseDevice = new Lazy<MouseDevice>(() => new MouseDevice());
        public KeyboardDevice KeyboardDevice => _keyboardDevice.Value;
        public MouseDevice MouseDevice => _mouseDevice.Value;
        public Dictionary<IntPtr, Action<XEvent>> Windows = new Dictionary<IntPtr, Action<XEvent>>();
        public XI2Manager XI2;
        public X11Info Info { get; private set; }
        public IX11Screens X11Screens { get; private set; }
        public IScreenImpl Screens { get; private set; }
        public X11PlatformOptions Options { get; private set; }
        public void Initialize(X11PlatformOptions options)
        {
            Options = options;
            XInitThreads();
            Display = XOpenDisplay(IntPtr.Zero);
            DeferredDisplay = XOpenDisplay(IntPtr.Zero);
            if (Display == IntPtr.Zero)
                throw new Exception("XOpenDisplay failed");
            XError.Init();
            Info = new X11Info(Display, DeferredDisplay);

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IPlatformThreadingInterface>().ToConstant(new X11PlatformThreading(this))
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(InputModifiers.Control))
                .Bind<IKeyboardDevice>().ToFunc(() => KeyboardDevice)
                .Bind<IStandardCursorFactory>().ToConstant(new X11CursorFactory(Display))
                .Bind<IClipboard>().ToConstant(new X11Clipboard(this))
                .Bind<IPlatformSettings>().ToConstant(new PlatformSettingsStub())
                .Bind<IPlatformIconLoader>().ToConstant(new X11IconLoader(Info))
                .Bind<ISystemDialogImpl>().ToConstant(new GtkSystemDialog());
            
            X11Screens = Avalonia.X11.X11Screens.Init(this);
            Screens = new X11Screens(X11Screens);
            if (Info.XInputVersion != null)
            {
                var xi2 = new XI2Manager();
                if (xi2.Init(this))
                    XI2 = xi2;
            }

            if (options.UseGpu)
            {
                if (options.UseEGL)
                    EglGlPlatformFeature.TryInitialize();
                else
                    GlxGlPlatformFeature.TryInitialize(Info);
            }

            
        }

        public IntPtr DeferredDisplay { get; set; }
        public IntPtr Display { get; set; }
        public IWindowImpl CreateWindow()
        {
            return new X11Window(this, false);
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }

        public IPopupImpl CreatePopup()
        {
            return new X11Window(this, true);
        }
    }
}

namespace Avalonia
{

    public class X11PlatformOptions
    {
        public bool UseEGL { get; set; }
        public bool UseGpu { get; set; } = true;

        public List<string> GlxRendererBlacklist { get; set; } = new List<string>
        {
            // llvmpipe is a software GL rasterizer. If it's returned by glGetString,
            // that usually means that something in the system is horribly misconfigured
            // and sometimes attempts to use GLX might cause a segfault
            "llvmpipe"
        };
        public string WmClass { get; set; } = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "AvaloniaApplication";
        public bool? EnableMultiTouch { get; set; }
    }
    public static class AvaloniaX11PlatformExtensions
    {
        public static T UseX11<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(() =>
                new AvaloniaX11Platform().Initialize(AvaloniaLocator.Current.GetService<X11PlatformOptions>() ??
                                                     new X11PlatformOptions()));
            return builder;
        }

        public static void InitializeX11Platform(X11PlatformOptions options = null) =>
            new AvaloniaX11Platform().Initialize(options ?? new X11PlatformOptions());
    }

}
