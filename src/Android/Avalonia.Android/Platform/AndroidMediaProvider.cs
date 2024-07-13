using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Android.Platform
{
    internal class AndroidMediaProvider : IMediaProvider
    {
        private readonly TopLevelImpl _topLevelImpl;

        public AndroidMediaProvider(TopLevelImpl topLevelImpl)
        {
            _topLevelImpl = topLevelImpl;


            if(_topLevelImpl.View.Context is IActivityConfigurationService activity)
            {
                activity.ConfigurationChanged += Activity_ConfigurationChanged;
            }
        }

        private void Activity_ConfigurationChanged(object? sender, EventArgs e)
        {
            OrientationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseSizeChanged()
        {
            ScreenSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? ScreenSizeChanged;
        public event EventHandler? OrientationChanged;

        public double GetScreenHeight() => _topLevelImpl.Size.Height;

        public double GetScreenWidth() => _topLevelImpl.Size.Width;

        public MediaOrientation GetDeviceOrientation()
        {
            return _topLevelImpl.View.Resources?.Configuration?.Orientation switch
            {
                global::Android.Content.Res.Orientation.Landscape => MediaOrientation.Landscape,
                global::Android.Content.Res.Orientation.Portrait => MediaOrientation.Portrait,
                global::Android.Content.Res.Orientation.Square => MediaOrientation.Square,
                _ => MediaOrientation.Portrait,
            };
        }

        public string GetPlatform()
        {
            return "android";
        }
    }
}
