using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class AvaloniaNativeCursor : ICursorImpl, IDisposable
    {
        public IAvnCursor Cursor { get; private set; }
        public IntPtr Handle => IntPtr.Zero;

        public string HandleDescriptor => "<none>";

        public AvaloniaNativeCursor(IAvnCursor cursor)
        {
            Cursor = cursor;
        }

        public void Dispose()
        {
            Cursor.Dispose();
            Cursor = null;
        }
    }

    class CursorFactory : ICursorFactory
    {
        IAvnCursorFactory _native;

        public CursorFactory(IAvnCursorFactory native)
        {
            _native = native;
        }

        public ICursorImpl GetCursor(StandardCursorType cursorType)
        {
            var cursor = _native.GetCursor((AvnStandardCursorType)cursorType);
            return new AvaloniaNativeCursor( cursor );
        }

        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            throw new NotImplementedException();
        }
    }
}
