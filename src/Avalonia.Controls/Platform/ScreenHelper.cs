using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Platform
{
    internal static class ScreenHelper
    {
        public static Screen? ScreenFromPoint(PixelPoint point, IReadOnlyList<Screen> screens)
        {
            foreach (Screen screen in screens)
            {
                if (screen.Bounds.ContainsExclusive(point))
                {
                    return screen;
                }
            }

            return null;
        }

        public static Screen? ScreenFromRect(PixelRect bounds, IReadOnlyList<Screen> screens)
        {
            Screen? currMaxScreen = null;
            double maxAreaSize = 0;

            foreach (Screen screen in screens)
            {
                double left = MathUtilities.Clamp(bounds.X, screen.Bounds.X, screen.Bounds.X + screen.Bounds.Width);
                double top = MathUtilities.Clamp(bounds.Y, screen.Bounds.Y, screen.Bounds.Y + screen.Bounds.Height);
                double right = MathUtilities.Clamp(bounds.X + bounds.Width, screen.Bounds.X, screen.Bounds.X + screen.Bounds.Width);
                double bottom = MathUtilities.Clamp(bounds.Y + bounds.Height, screen.Bounds.Y, screen.Bounds.Y + screen.Bounds.Height);
                double area = (right - left) * (bottom - top);
                if (area > maxAreaSize)
                {
                    maxAreaSize = area;
                    currMaxScreen = screen;
                }
            }

            return currMaxScreen;
        }

        public static Screen? ScreenFromWindow(IWindowBaseImpl window, IReadOnlyList<Screen> screens)
        {
            var rect = new PixelRect(
                window.Position, 
                PixelSize.FromSize(window.FrameSize ?? window.ClientSize, window.DesktopScaling));

            return ScreenFromRect(rect, screens);
        }
    }
}
