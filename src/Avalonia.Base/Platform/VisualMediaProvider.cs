using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Reactive;

namespace Avalonia.Platform
{
    internal class VisualMediaProvider : IMediaProvider
    {
        private readonly Visual _visual;

        public VisualMediaProvider(Visual visual)
        {
            _visual = visual;

            _visual.PropertyChanged += Visual_PropertyChanged;
        }

        private void Visual_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == Visual.BoundsProperty)
            {
                var oldValue = e.GetOldValue<Rect>();
                var newValue = e.GetNewValue<Rect>();

                if (oldValue.Size != newValue.Size)
                {
                    ScreenSizeChanged?.Invoke(_visual, EventArgs.Empty);
                    OrientationChanged?.Invoke(_visual, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? ScreenSizeChanged;
        public event EventHandler? OrientationChanged;

        public double GetScreenHeight() => _visual.Bounds.Size.Height;

        public double GetScreenWidth() => _visual.Bounds.Size.Width;

        public DeviceOrientation GetDeviceOrientation()
        {
            var width = GetScreenWidth();
            var height = GetScreenHeight();

            if(width > height)
                return DeviceOrientation.Landscape;

            if(height > width)
                return DeviceOrientation.Portrait;

            return DeviceOrientation.Square;
        }

        public string GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx";

            return "";
        }
    }
}
