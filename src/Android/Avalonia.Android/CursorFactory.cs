using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Android
{
    internal class CursorFactory : ICursorFactory
    {
        public ICursorImpl CreateCursor(Avalonia.Media.Imaging.Bitmap cursor, PixelPoint hotSpot) => CursorImpl.ZeroCursor;

        public ICursorImpl GetCursor(StandardCursorType cursorType) => CursorImpl.ZeroCursor;

        private sealed class CursorImpl : ICursorImpl
        {
            public static CursorImpl ZeroCursor { get; } = new CursorImpl();

            private CursorImpl() { }

            public void Dispose() { }
        }
    }
}
