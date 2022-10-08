using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents all screens available on a device.
    /// </summary>
    public class Screens
    {
        private readonly IScreenImpl _iScreenImpl;

        /// <summary>
        /// Gets the total number of screens available on this device.
        /// </summary>
        public int ScreenCount => _iScreenImpl?.ScreenCount ?? 0;

        /// <summary>
        /// Gets the list of all screens available on this device.
        /// </summary>
        public IReadOnlyList<Screen> All => _iScreenImpl?.AllScreens ?? Array.Empty<Screen>();

        /// <summary>
        /// Gets the primary screen on this device.
        /// </summary>
        public Screen? Primary => All.FirstOrDefault(x => x.IsPrimary);

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
