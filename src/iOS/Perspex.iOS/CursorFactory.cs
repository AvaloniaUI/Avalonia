using System;
using Perspex.Input;
using Perspex.Platform;

namespace Perspex.iOS
{
    class CursorFactory : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType) => new PlatformHandle(IntPtr.Zero, "NULL");
    }
}