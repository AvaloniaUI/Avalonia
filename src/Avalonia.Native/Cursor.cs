using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class AvaloniaNativeCursor : IPlatformHandle, IDisposable
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

    class CursorFactory : IStandardCursorFactory
    {
        IAvnCursorFactory _native;

        public CursorFactory(IAvnCursorFactory native)
        {
            _native = native;
        }

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            var cursor = _native.GetCursor((AvnStandardCursorType)cursorType);
            return new AvaloniaNativeCursor( cursor );
        }
    }
}
