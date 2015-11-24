using Perspex.Android.CanvasRendering;
using Perspex.Android.Platform;
using Perspex.Android.Platform.Input;
using Perspex.Android.Platform.Specific;
using Perspex.Controls.Platform;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using Perspex.Skia;
using System;
using System.Collections.Generic;

namespace Perspex.Android
{
    public class AndroidPlatform : IPlatformSettings
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);
        public double RenderScalingFactor => _scalingFactor;
        public double LayoutScalingFactor => _scalingFactor;

        private readonly double _scalingFactor = 1;

        AndroidPlatform()
        {
            PerspexLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IMouseDevice>().ToSingleton<AndroidMouseDevice>()
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<ITopLevelRenderer>().ToTransient<AndroidTopLevelRenderer>();

            SkiaPlatform.Initialize();
            Application.RegisterPlatformCallback(() => { });
            PerspexLocator.CurrentMutable.Bind<IWindowImpl>().ToSingleton<Platform.SkiaPlatform.MainWindowImpl>();

            _scalingFactor = global::Android.App.Application.Context.Resources.DisplayMetrics.ScaledDensity;

            
            //we have custom Assetloader so no need to overwrite it
            
        }

        public void Init(Type applicationType)
        {
            SharedPlatform.Register(applicationType.Assembly);
        }
    }
}