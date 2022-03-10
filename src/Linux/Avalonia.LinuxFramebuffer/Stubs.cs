using System;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer
{
    internal class CursorFactoryStub : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub();
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new CursorStub();

        private class CursorStub : ICursorImpl
        {
            public void Dispose() { }
        }
    }
    internal class PlatformSettings : IPlatformSettings
    {
        public Size DoubleClickSize { get; } = new Size(4, 4);
        public TimeSpan DoubleClickTime { get; } = new TimeSpan(0, 0, 0, 0, 500);

        public Size TouchDoubleClickSize => new Size(16,16);

        public TimeSpan TouchDoubleClickTime => DoubleClickTime;
    }
}
