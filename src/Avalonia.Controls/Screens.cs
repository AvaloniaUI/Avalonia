using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    public class Screens
    {
        private readonly IScreenImpl _impl;

        public int ScreenCount => _impl.ScreenCount;
        public IReadOnlyList<Screen> All => _impl?.AllScreens ?? Array.Empty<Screen>();
        public Screen? Primary => All.FirstOrDefault(x => x.Primary);

        public Screens(IScreenImpl impl)
        {
            _impl = impl;
        }

        public Screen? ScreenFromBounds(PixelRect bounds)
        {
            return _impl.ScreenFromRect(bounds);
        }
        
        public Screen? ScreenFromWindow(IWindowBaseImpl window)
        {
            return _impl.ScreenFromWindow(window);
        }

        public Screen? ScreenFromPoint(PixelPoint point)
        {
            return _impl.ScreenFromPoint(point);
        }

        public Screen? ScreenFromVisual(IVisual visual)
        {
            var tl = visual.PointToScreen(visual.Bounds.TopLeft);
            var br = visual.PointToScreen(visual.Bounds.BottomRight);

            return ScreenFromBounds(new PixelRect(tl, br));
        }
    }
}
