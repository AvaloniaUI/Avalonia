using Avalonia.Input;
using Avalonia.Metadata;

#nullable enable

namespace Avalonia.Platform
{
    [PrivateApi]
    public interface ICursorFactory
    {
        ICursorImpl GetCursor(StandardCursorType cursorType);
        ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot);
    }
}
