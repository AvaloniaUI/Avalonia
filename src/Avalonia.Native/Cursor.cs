using System;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class Cursor : IPlatformHandle
    {
        public IntPtr Handle => IntPtr.Zero;

        public string HandleDescriptor => "STUB";
    }

    class CursorFactoryStub : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new Cursor();
        }
    }
}
