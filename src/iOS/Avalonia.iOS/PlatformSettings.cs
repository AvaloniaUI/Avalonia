using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Platform;
using UIKit;

namespace Avalonia.iOS
{
    class PlatformSettings : IPlatformSettings
    {
        public Size DoubleClickSize =>new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);
        public double RenderScalingFactor => UIScreen.MainScreen.Scale;
        public double LayoutScalingFactor => 1;
    }
}
