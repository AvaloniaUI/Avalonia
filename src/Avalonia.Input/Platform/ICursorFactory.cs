using Avalonia.Input;

#nullable enable

namespace Avalonia.Platform
{
    public interface ICursorFactory
    {
        IPlatformHandle GetCursor(StandardCursorType cursorType);
        IPlatformHandle CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot);
    }
}
