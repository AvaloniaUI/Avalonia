using System;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Controls.UnitTests
{
    public class CursorFactoryMock : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new PlatformHandle(IntPtr.Zero, cursorType.ToString());
        }

        public IPlatformHandle CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            return new PlatformHandle(IntPtr.Zero, "Custom");
        }
    }
}
