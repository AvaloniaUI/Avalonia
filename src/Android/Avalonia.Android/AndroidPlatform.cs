using System;

using Avalonia.Android;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Skia;

namespace Avalonia
{
    public static class AndroidApplicationExtensions
    {
        public static T UseAndroid<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            var options = AvaloniaLocator.Current.GetService<AndroidPlatformOptions>() ?? new AndroidPlatformOptions();
            builder.UseWindowingSubsystem(() => AndroidPlatform.Initialize(builder.ApplicationType, options), "Android");
            builder.UseSkia();
            return builder;
        }
    }
}

namespace Avalonia.Android
{
    class AndroidPlatform : IPlatformSettings
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public static AndroidPlatformOptions Options { get; private set; }
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);

        public static void Initialize(Type appType, AndroidPlatformOptions options)
        {
            Options = options;

            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoader>()
                .Bind<IRenderTimer>().ToConstant(new ChoreographerTimer())
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(appType.Assembly));

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
