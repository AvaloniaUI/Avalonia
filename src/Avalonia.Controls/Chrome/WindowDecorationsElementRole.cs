namespace Avalonia.Input;

/// <summary>
/// Defines the cross-platform role of a visual element for non-client hit-testing.
/// Used to mark elements as titlebar drag areas, resize grips, etc.
/// </summary>
public enum WindowDecorationsElementRole
{
    /// <summary>
    /// No special role. The element participates in normal input handling.
    /// </summary>
    None,

    /// <summary>
    /// The element acts as a titlebar drag area.
    /// Clicking and dragging on this element initiates a platform window move.
    /// </summary>
    TitleBar,

    /// <summary>
    /// Resize grip for the north (top) edge.
    /// </summary>
    ResizeN,

    /// <summary>
    /// Resize grip for the south (bottom) edge.
    /// </summary>
    ResizeS,

    /// <summary>
    /// Resize grip for the east (right) edge.
    /// </summary>
    ResizeE,

    /// <summary>
    /// Resize grip for the west (left) edge.
    /// </summary>
    ResizeW,

    /// <summary>
    /// Resize grip for the northeast corner.
    /// </summary>
    ResizeNE,

    /// <summary>
    /// Resize grip for the northwest corner.
    /// </summary>
    ResizeNW,

    /// <summary>
    /// Resize grip for the southeast corner.
    /// </summary>
    ResizeSE,

    /// <summary>
    /// Resize grip for the southwest corner.
    /// </summary>
    ResizeSW
}
