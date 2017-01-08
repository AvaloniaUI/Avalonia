using System;
using System.IO;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Skia;

namespace Avalonia
{
    public static class AndroidApplicationExtensions
    {
        public static T UseAndroid<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(Android.AndroidPlatform.Initialize, "Android");
            return builder;
        }
    }
}

namespace Avalonia.Android
{
    public class AndroidPlatform : IPlatformSettings, IWindowingPlatform
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

        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IMouseDevice>().ToSingleton<AndroidMouseDevice>()
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IWindowingPlatform>().ToConstant(Instance)
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoader>();

            SkiaPlatform.Initialize();
        }

        public void Init(Type applicationType)
        {
            StandardRuntimePlatformServices.Register(applicationType.Assembly);
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl();
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }
    }
}