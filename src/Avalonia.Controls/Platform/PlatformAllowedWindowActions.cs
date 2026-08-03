using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Flags indicating which window actions the underlying platform supports.
/// </summary>
[Flags, PrivateApi]
public enum PlatformAllowedWindowActions
{
    None = 0,

    /// <summary>
    /// The underlying platform supports maximizing/unmaximizing windows.
    /// </summary>
    Maximize = 1 << 0,

    /// <summary>
    /// The underlying platform supports fullscreen mode.
    /// </summary>
    Fullscreen = 1 << 1,

    /// <summary>
    /// The underlying platform supports minimizing windows.
    /// </summary>
    Minimize = 1 << 2,

    /// <summary>
    /// All actions are supported (default when the underlying platform does not report capabilities).
    /// </summary>
    All = Maximize | Fullscreen | Minimize,
}
