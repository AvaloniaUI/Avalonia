using Avalonia.Input;

namespace Avalonia.Platform
{
    public interface IStandardCursorFactory
    {
        IPlatformHandle GetCursor(StandardCursorType cursorType);
    }
}
