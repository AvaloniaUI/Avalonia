using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer
{
    internal class CursorFactoryStub : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new PlatformHandle(IntPtr.Zero, null);
        }
    }
    internal class PlatformSettings : IPlatformSettings
    {
        public Size DoubleClickSize { get; } = new Size(4, 4);
        public TimeSpan DoubleClickTime { get; } = new TimeSpan(0, 0, 0, 0, 500);
    }
}
