using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia
{
    public static class IOSApplicationExtensions
    {
        public static T UseiOS<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            return builder
                .UseWindowingSubsystem(iOS.Platform.Register, "iOS")
                .UseSkia();
        }
    }
}

namespace Avalonia.iOS
{
    static class Platform
    {
        public static EaglFeature GlFeature;
        public static DisplayLinkTimer Timer;
        internal static Compositor Compositor { get; private set; }
        
        class PlatformSettings : IPlatformSettings
        {
            /// <inheritdoc cref="IPlatformSettings.TouchDoubleClickSize"/>
            public Size TouchDoubleClickSize => new Size(10, 10);

            /// <inheritdoc cref="IPlatformSettings.TouchDoubleClickTime"/>
            public TimeSpan TouchDoubleClickTime => TimeSpan.FromMilliseconds(500);

            public Size DoubleClickSize => new Size(4, 4);

            public TimeSpan DoubleClickTime => TouchDoubleClickTime;
        }

        public static void Register()
        {
            GlFeature ??= new EaglFeature();
            Timer ??= new DisplayLinkTimer();
            var keyboard = new KeyboardDevice();
            
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformOpenGlInterface>().ToConstant(GlFeature)
                .Bind<ICursorFactory>().ToConstant(new CursorFactoryStub())
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IClipboard>().ToConstant(new ClipboardImpl())
                .Bind<IPlatformSettings>().ToConstant(new PlatformSettings())
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoaderStub())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderLoop>().ToSingleton<RenderLoop>()
                .Bind<IRenderTimer>().ToConstant(Timer)
                .Bind<IPlatformThreadingInterface>().ToConstant(new PlatformThreadingInterface())
                .Bind<IKeyboardDevice>().ToConstant(keyboard);

                Compositor = new Compositor(
                    AvaloniaLocator.Current.GetRequiredService<IRenderLoop>(),
                    AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>());
        }


    }
}

