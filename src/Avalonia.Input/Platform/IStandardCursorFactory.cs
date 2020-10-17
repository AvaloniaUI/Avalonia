using Avalonia.Input;

#nullable enable

namespace Avalonia.Platform
{
    public interface IStandardCursorFactory
    {
        IPlatformHandle GetCursor(StandardCursorType cursorType);
    }
}
