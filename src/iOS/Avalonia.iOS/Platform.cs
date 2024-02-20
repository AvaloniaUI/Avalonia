using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia
{
    public static class IOSApplicationExtensions
    {
        public static AppBuilder UseiOS(this AppBuilder builder)
        {
            return builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(iOS.Platform.Register, "iOS")
                .UseSkia();
        }
    }
}

namespace Avalonia.iOS
{
    static class Platform
    {
        public static EaglPlatformGraphics GlFeature;
        public static DisplayLinkTimer Timer;
        internal static Compositor Compositor { get; private set; }

        public static void Register()
        {
            GlFeature ??= new EaglPlatformGraphics();
            Timer ??= new DisplayLinkTimer();
            var keyboard = new KeyboardDevice();

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformGraphics>().ToConstant(GlFeature)
                .Bind<ICursorFactory>().ToConstant(new CursorFactoryStub())
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoaderStub())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderTimer>().ToConstant(Timer)
                .Bind<IDispatcherImpl>().ToConstant(DispatcherImpl.Instance)
                .Bind<IKeyboardDevice>().ToConstant(keyboard);

                Compositor = new Compositor(AvaloniaLocator.Current.GetService<IPlatformGraphics>());
            	AvaloniaLocator.CurrentMutable.Bind<Compositor>().ToConstant(Compositor);
        }
    }
}

