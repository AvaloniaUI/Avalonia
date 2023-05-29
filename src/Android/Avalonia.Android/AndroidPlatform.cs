using System;
using Avalonia.Controls;
using Avalonia.Android;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.OpenGL;

namespace Avalonia
{
    public static class AndroidApplicationExtensions
    {
        public static AppBuilder UseAndroid(this AppBuilder builder)
        {
            return builder
                .UseWindowingSubsystem(() => AndroidPlatform.Initialize(), "Android")
                .UseSkia();
        }
    }
}

namespace Avalonia.Android
{
    class AndroidPlatform
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public static AndroidPlatformOptions Options { get; private set; }

        internal static Compositor Compositor { get; private set; }

        public static void Initialize()
        {
            Options = AvaloniaLocator.Current.GetService<AndroidPlatformOptions>() ?? new AndroidPlatformOptions();

            AvaloniaLocator.CurrentMutable
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToSingleton<AndroidPlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
                .Bind<IRenderTimer>().ToConstant(new ChoreographerTimer())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

            if (Options.UseGpu)
            {
                EglPlatformGraphics.TryInitialize();
            }
            
            Compositor = new Compositor(AvaloniaLocator.Current.GetService<IPlatformGraphics>());
        }
    }

    public sealed class AndroidPlatformOptions
    {
        public bool UseDeferredRendering { get; set; } = false;
        public bool UseGpu { get; set; } = true;
    }
}
