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
    class AndroidPlatform : IPlatformSettings, IWindowingPlatform
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);
        public double RenderScalingFactor => _scalingFactor;
        public double LayoutScalingFactor => _scalingFactor;

        private readonly double _scalingFactor = 1;

        public AndroidPlatform()
        {
            _scalingFactor = global::Android.App.Application.Context.Resources.DisplayMetrics.ScaledDensity;
        }

        public static void Initialize(Type appType, AndroidPlatformOptions options)
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IWindowingPlatform>().ToConstant(Instance)
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoader>()
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(appType.Assembly));

            SkiaPlatform.Initialize();
            ((global::Android.App.Application) global::Android.App.Application.Context.ApplicationContext)
                .RegisterActivityLifecycleCallbacks(new ActivityTracker());

            if (options.UseGpu)
            {
                EglPlatformOpenGlInterface.TryInitialize();
            }
        }

        public IWindowImpl CreateWindow()
        {
            throw new NotSupportedException();
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }
    }

    public sealed class AndroidPlatformOptions
    {
        public bool UseGpu { get; set; } = true;
    }
}
