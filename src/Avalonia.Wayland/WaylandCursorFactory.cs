using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Wayland;

class WaylandCursorFactory : ICursorFactory
{
    public ICursorImpl GetCursor(StandardCursorType cursorType) => new WaylandCursorImpl(cursorType);

    public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot) => new WaylandCursorImpl(StandardCursorType.Arrow);
}

class WaylandCursorImpl : ICursorImpl
{
    public StandardCursorType CursorType { get; }

    public WaylandCursorImpl(StandardCursorType cursorType)
    {
        CursorType = cursorType;
    }

    public void Dispose()
    {
    }
}
