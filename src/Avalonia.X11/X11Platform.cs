using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.X11;
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
        public X11Info Info { get; private set; }
        public void Initialize()
        {
            XInitThreads();
            Display = XOpenDisplay(IntPtr.Zero);
            DeferredDisplay = XOpenDisplay(IntPtr.Zero);
            if (Display == IntPtr.Zero)
                throw new Exception("XOpenDisplay failed");
            XError.Init();
            Info = new X11Info(Display, DeferredDisplay);

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IPlatformThreadingInterface>().ToConstant(new X11PlatformThreading(Display, Windows))
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(InputModifiers.Control))
                .Bind<IKeyboardDevice>().ToFunc(() => KeyboardDevice)
                .Bind<IStandardCursorFactory>().ToConstant(new X11CursorFactory(Display))
                .Bind<IClipboard>().ToSingleton<ClipboardStub>()
                .Bind<IPlatformSettings>().ToConstant(new PlatformSettingsStub())
                .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogsStub())
                .Bind<IPlatformIconLoader>().ToConstant(new IconLoaderStub());
            EglGlPlatformFeature.TryInitialize();

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
    public static class AvaloniaX11PlatformExtensions
    {
        public static T UseX11<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(() => new AvaloniaX11Platform().Initialize());
            return builder;
        }

        public static void InitializeX11Platform() => new AvaloniaX11Platform().Initialize();
    }

}
