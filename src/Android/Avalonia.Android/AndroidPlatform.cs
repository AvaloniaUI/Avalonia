using System;
using System.IO;
using Avalonia.Android.CanvasRendering;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia
{
    public static class AndroidApplicationExtensions
    {
        public static AppBuilder UseAndroid(this AppBuilder builder)
        {
            builder.WindowingSubsystem = Avalonia.Android.AndroidPlatform.Initialize;
            return builder;
        }
    }
}

namespace Avalonia.Android
{
    public class AndroidPlatform : IPlatformSettings, IWindowingPlatform, IPlatformIconLoader
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
                .Bind<ITopLevelRenderer>().ToTransient<AndroidTopLevelRenderer>()
                .Bind<IWindowingPlatform>().ToConstant(Instance);
        }

        public void Init(Type applicationType)
        {
            SharedPlatform.Register(applicationType.Assembly);
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl();
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            return null;
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return null;
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            return null;
        }
    }
}