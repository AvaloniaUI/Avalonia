using System;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Benchmarks
{
    internal class NullCursorFactory : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new PlatformHandle(IntPtr.Zero, "null");
        }
    }
}
