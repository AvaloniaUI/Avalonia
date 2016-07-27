using Avalonia.Android.CanvasRendering;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.Specific;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Skia;
using System;
using System.Collections.Generic;
using Avalonia.Android.Platform.SkiaPlatform;
using System.IO;

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

        AndroidPlatform()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IMouseDevice>().ToSingleton<AndroidMouseDevice>()
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<ITopLevelRenderer>().ToTransient<AndroidTopLevelRenderer>()
                .Bind<IWindowingPlatform>().ToConstant(this);

            SkiaPlatform.Initialize();

            _scalingFactor = global::Android.App.Application.Context.Resources.DisplayMetrics.ScaledDensity;

            
            //we have custom Assetloader so no need to overwrite it
            
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