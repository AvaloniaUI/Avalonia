using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class Screens
    {
        private readonly IScreenImpl _iScreenImpl;

        public int ScreenCount => _iScreenImpl.ScreenCount;
        public IReadOnlyList<Screen> All => _iScreenImpl?.AllScreens ?? Array.Empty<Screen>();
        public Screen Primary => All.FirstOrDefault(x => x.Primary);

        public Screens(IScreenImpl iScreenImpl)
        {
            _iScreenImpl = iScreenImpl;
        }

        public Screen ScreenFromBounds(PixelRect bounds){
        
            Screen currMaxScreen = null;
            double maxAreaSize = 0;
            foreach (Screen screen in All)
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
        
        public Screen ScreenFromPoint(PixelPoint point)
        {
            return All.FirstOrDefault(x => x.Bounds.Contains(point));        
        }

        public Screen ScreenFromVisual(IVisual visual)
        {
            var tl = visual.PointToScreen(visual.Bounds.TopLeft);
            var br = visual.PointToScreen(visual.Bounds.BottomRight);
            return ScreenFromBounds(new PixelRect(tl, br));
        }
    }
}
