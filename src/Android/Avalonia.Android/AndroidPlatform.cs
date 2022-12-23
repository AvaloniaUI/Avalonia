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
        public static T UseAndroid<T>(this T builder) where T : AppBuilderBase<T>, new()
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
        internal static PlatformRenderInterfaceContextManager RenderInterface { get; private set; }

        public static void Initialize()
        {
            Options = AvaloniaLocator.Current.GetService<AndroidPlatformOptions>() ?? new AndroidPlatformOptions();

            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
                .Bind<IRenderTimer>().ToConstant(new ChoreographerTimer())
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

            if (Options.UseGpu)
            {
                EglPlatformGraphics.TryInitialize();
            }
            
            if (Options.UseCompositor)
            {
                Compositor = new Compositor(
                    AvaloniaLocator.Current.GetRequiredService<IRenderLoop>(),
                    AvaloniaLocator.Current.GetService<IPlatformGraphics>());
            }
            else
                RenderInterface =
                    new PlatformRenderInterfaceContextManager(AvaloniaLocator.Current
                        .GetService<IPlatformGraphics>());
        }
    }

    public sealed class AndroidPlatformOptions
    {
        public bool UseDeferredRendering { get; set; } = false;
        public bool UseGpu { get; set; } = true;
        public bool UseCompositor { get; set; } = true;
    }
}
