using System;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal unsafe class WlThemeCursor : WlCursor
    {
        private readonly LibWaylandCursor.wl_cursor* _wlCursor;
        private readonly WlCursorImage?[] _wlCursorImages;

        public WlThemeCursor(LibWaylandCursor.wl_cursor* wlCursor) : base(wlCursor->image_count)
        {
            _wlCursor = wlCursor;
            _wlCursorImages = new WlCursorImage[ImageCount];
        }

        public override WlCursorImage this[int index]
        {
            get
            {
                var cachedImage = _wlCursorImages[index];
                if (cachedImage is not null)
                    return cachedImage;
                var image = _wlCursor->images[index];
                var bufferPtr = LibWaylandCursor.wl_cursor_image_get_buffer(image);
                var wlBuffer = new WlBuffer(bufferPtr, WlBuffer.InterfaceVersion);
                var size = new PixelSize((int)image->width, (int)image->height);
                var hotspot = new PixelPoint((int)image->hotspot_x, (int)image->hotspot_y);
                var delay = TimeSpan.FromMilliseconds(image->delay);
                return _wlCursorImages[index] = new WlCursorImage(wlBuffer, size, hotspot, delay);
            }
        }

        public override void Dispose()
        {
            foreach (var wlCursorImage in _wlCursorImages)
                wlCursorImage?.WlBuffer.Dispose();
        }
    }
}
