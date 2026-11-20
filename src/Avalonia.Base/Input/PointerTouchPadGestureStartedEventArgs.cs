using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Provides data for the events raised when a continuous touchpad gesture starts, such as
/// <see cref="InputElement.PointerTouchPadGestureMagnifyStartedEvent"/>.
/// </summary>
/// <remarks>
/// Until the matching ended event, all the events of the gesture are raised on the element
/// that received this event, even if the pointer is no longer over it.
/// </remarks>
public class PointerTouchPadGestureStartedEventArgs(
    RoutedEvent? routedEvent,
    object? source,
    IPointer pointer,
    Visual rootVisual,
    Point rootVisualPosition,
    ulong timestamp,
    PointerPointProperties properties,
    KeyModifiers modifiers)
    : PointerEventArgs(routedEvent, source, pointer, rootVisual, rootVisualPosition, timestamp, properties, modifiers);
