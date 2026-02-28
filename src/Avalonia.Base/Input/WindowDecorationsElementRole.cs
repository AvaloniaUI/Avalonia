namespace Avalonia.Input;

/// <summary>
/// Defines the cross-platform role of a visual element for non-client hit-testing.
/// Used to mark elements as titlebar drag areas, resize grips, etc.
/// </summary>
public enum WindowDecorationsElementRole
{
    /// <summary>
    /// No special role. The element is invisible to chrome hit-testing.
    /// </summary>
    None,

    /// <summary>
    /// An interactive element that is part of the decorations chrome (e.g., a caption button).
    /// Set by themes on decoration template elements. Input is passed through to the element
    /// rather than being intercepted for non-client actions.
    /// </summary>
    DecorationsElement,

    /// <summary>
    /// An interactive element set by user code that should receive input even when
    /// overlapping chrome areas. Has the same effect as <see cref="DecorationsElement"/>
    /// but is intended for use by application developers.
    /// </summary>
    User,

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
    ResizeSW,

    /// <summary>
    /// The element acts as the window close button.
    /// On Win32, maps to HTCLOSE for system close behavior.
    /// On other platforms, treated as an interactive decoration element.
    /// </summary>
    CloseButton,

    /// <summary>
    /// The element acts as the window minimize button.
    /// On Win32, maps to HTMINBUTTON for system minimize behavior.
    /// On other platforms, treated as an interactive decoration element.
    /// </summary>
    MinimizeButton,

    /// <summary>
    /// The element acts as the window maximize/restore button.
    /// On Win32, maps to HTMAXBUTTON for system maximize behavior.
    /// On other platforms, treated as an interactive decoration element.
    /// </summary>
    MaximizeButton,

    /// <summary>
    /// The element acts as the window fullscreen toggle button.
    /// Treated as an interactive decoration element on all platforms.
    /// </summary>
    FullScreenButton
}
