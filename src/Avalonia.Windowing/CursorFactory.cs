using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class CursorFactory : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType) => new DummyPlatformHandle();
    }
}
