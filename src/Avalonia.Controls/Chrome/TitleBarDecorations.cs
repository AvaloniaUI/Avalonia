using System;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Flags specifying which elements are displayed in a drawn window title bar of <see cref="WindowDrawnDecorations"/>.
/// </summary>
[Flags]
public enum TitleBarDecorations
{
    /// <summary>
    /// No title bar element is displayed.
    /// </summary>
    None = 0,

    /// <summary>
    /// The window title is displayed.
    /// </summary>
    Title = 1 << 0,

    /// <summary>
    /// The minimize button is displayed.
    /// </summary>
    MinimizeButton = 1 << 1,

    /// <summary>
    /// The maximize button is displayed.
    /// </summary>
    MaximizeButton = 1 << 2,

    /// <summary>
    /// The close button is displayed.
    /// </summary>
    CloseButton = 1 << 3,

    /// <summary>
    /// The full screen button is displayed.
    /// </summary>
    FullScreenButton = 1 << 4,

    /// <summary>
    /// All title bar elements are displayed.
    /// </summary>
    All = Title | MinimizeButton | MaximizeButton | CloseButton | FullScreenButton
}
