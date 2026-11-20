using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Provides data for the events raised when a continuous touchpad gesture ends, such as
/// <see cref="InputElement.PointerTouchPadGestureMagnifyEndedEvent"/>.
/// </summary>
public class PointerTouchPadGestureEndedEventArgs(
    RoutedEvent? routedEvent,
    object? source,
    IPointer pointer,
    Visual rootVisual,
    Point rootVisualPosition,
    ulong timestamp,
    PointerPointProperties properties,
    KeyModifiers modifiers)
    : PointerEventArgs(routedEvent, source, pointer, rootVisual, rootVisualPosition, timestamp, properties, modifiers)
{
    /// <summary>
    /// Gets a value indicating whether the gesture was canceled by the system instead of being completed by the user.
    /// </summary>
    public bool IsCanceled { get; init; }
}
