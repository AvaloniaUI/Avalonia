using System;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Flags controlling which parts of drawn window decorations are active.
/// Set by Window based on platform capabilities and user preferences.
/// </summary>
[Flags]
internal enum DrawnWindowDecorationParts
{
    /// <summary>
    /// No decoration parts are active.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shadow/outer area is active.
    /// </summary>
    Shadow = 1,

    /// <summary>
    /// Frame border is active.
    /// </summary>
    Border = 2,

    /// <summary>
    /// Titlebar is active.
    /// </summary>
    TitleBar = 4,

    /// <summary>
    /// Resize grips are active.
    /// </summary>
    ResizeGrips = 8,

    /// <summary>
    /// All decoration parts are active.
    /// </summary>
    All = Shadow | Border | TitleBar | ResizeGrips
}
