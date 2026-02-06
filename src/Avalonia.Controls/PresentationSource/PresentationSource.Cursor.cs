using Avalonia.Input;

namespace Avalonia.Controls;

internal partial class PresentationSource
{
    private Cursor? _cursor;
    private Cursor? _cursorOverride;

    private void UpdateCursor() => PlatformImpl?.SetCursor(_cursorOverride?.PlatformImpl ?? _cursor?.PlatformImpl);

    private void SetCursor(Cursor? cursor)
    {
        _cursor = cursor;
        UpdateCursor();
    }
    
    /// <summary>
    /// This should only be used by InProcessDragSource
    /// </summary>
    internal void SetCursorOverride(Cursor? cursor)
    {
        _cursorOverride = cursor;
        UpdateCursor();
    }
}