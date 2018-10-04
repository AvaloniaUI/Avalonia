using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class Cursor : IPlatformHandle
    {
        public IntPtr Handle { get; }

        public string HandleDescriptor => "NSCursor";

        public Cursor(IntPtr handle)
        {
            Handle = handle;
        }
    }

    class CursorFactory : IStandardCursorFactory
    {
        IAvnCursor _native;

        public CursorFactory(IAvnCursor native)
        {
            _native = native;
        }

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            var handle = _native.GetCursor((AvnStandardCursorType)cursorType);
            return new Cursor( handle );
        }
    }
}
