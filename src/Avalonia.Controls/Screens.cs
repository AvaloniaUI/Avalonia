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
        private readonly IScreenImpl _iScreenImpl;

        public int ScreenCount => _iScreenImpl?.ScreenCount ?? 0;
        public IReadOnlyList<Screen> All => _iScreenImpl?.AllScreens ?? Array.Empty<Screen>();
        public Screen? Primary => All.FirstOrDefault(x => x.Primary);

        public Screens(IScreenImpl iScreenImpl)
        {
            _iScreenImpl = iScreenImpl;
        }

        public Screen? ScreenFromBounds(PixelRect bounds)
        {
            return _iScreenImpl.ScreenFromRect(bounds);
        }
        
        public Screen? ScreenFromWindow(IWindowBaseImpl window)
        {
            return _iScreenImpl.ScreenFromWindow(window);
        }

        public Screen? ScreenFromPoint(PixelPoint point)
        {      
            return _iScreenImpl.ScreenFromPoint(point);
        }

        public Screen? ScreenFromVisual(IVisual visual)
        {
            var tl = visual.PointToScreen(visual.Bounds.TopLeft);
            var br = visual.PointToScreen(visual.Bounds.BottomRight);

            return ScreenFromBounds(new PixelRect(tl, br));
        }
    }
}
