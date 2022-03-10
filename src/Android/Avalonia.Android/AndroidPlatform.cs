using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Android;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia;

namespace Avalonia
{
    public static class AndroidApplicationExtensions
    {
        public static T UseAndroid<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            var options = AvaloniaLocator.Current.GetService<AndroidPlatformOptions>() ?? new AndroidPlatformOptions();

            return builder
                .UseWindowingSubsystem(() => AndroidPlatform.Initialize(options), "Android")
                .UseSkia();
        }
    }
}

namespace Avalonia.Android
{
    class AndroidPlatform : IPlatformSettings
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public static AndroidPlatformOptions Options { get; private set; }

        /// <inheritdoc cref="IPlatformSettings.TouchDoubleClickSize"/>
        public Size TouchDoubleClickSize => new Size(4, 4);

        /// <inheritdoc cref="IPlatformSettings.TouchDoubleClickTime"/>
        public TimeSpan TouchDoubleClickTime => TimeSpan.FromMilliseconds(200);

        public Size DoubleClickSize => TouchDoubleClickSize;

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(500);

        public static void Initialize(AndroidPlatformOptions options)
        {
            Options = options;

            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
                .Bind<IRenderTimer>().ToConstant(new ChoreographerTimer())
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

            SkiaPlatform.Initialize();

            if (options.UseGpu)
            {
                EglPlatformOpenGlInterface.TryInitialize();
            }
        }
    }

    public sealed class AndroidPlatformOptions
    {
        public bool UseDeferredRendering { get; set; } = true;
        public bool UseGpu { get; set; } = true;
    }
}
