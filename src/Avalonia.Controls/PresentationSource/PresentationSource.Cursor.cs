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
    
    IInputElement? IInputRoot.PointerOverElement
    {
        get => field;
        set
        {
            if (field is AvaloniaObject old)
                old.PropertyChanged -= PointerOverElement_PropertyChanged;
            field = value;
            if (field is AvaloniaObject @new)
                @new.PropertyChanged -= PointerOverElement_PropertyChanged;
            SetCursor(value?.Cursor);
        }
    }

    private void PointerOverElement_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.CursorProperty)
            SetCursor((sender as IInputElement)?.Cursor);
    }
}