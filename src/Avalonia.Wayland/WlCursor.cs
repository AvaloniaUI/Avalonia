using System;
using Avalonia.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal abstract class WlCursor : ICursorImpl
    {
        protected WlCursor(uint imageCount)
        {
            ImageCount = imageCount;
        }

        public abstract WlCursorImage this[int index] { get; }

        public uint ImageCount { get; }

        public abstract void Dispose();

        public class WlCursorImage
        {
            public WlCursorImage(WlBuffer wlBuffer, PixelSize size, PixelPoint hotspot, TimeSpan delay)
            {
                WlBuffer = wlBuffer;
                Size = size;
                Hotspot = hotspot;
                Delay = delay;
            }

            public WlBuffer WlBuffer { get; }

            public PixelSize Size { get; }

            public PixelPoint Hotspot { get; }

            public TimeSpan Delay { get; }
        }
    }
}
