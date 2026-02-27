using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Flags indicating which drawn decoration parts a platform backend requires.
/// </summary>
[Flags, PrivateApi]
public enum PlatformRequstedDrawnDecoration
{
    None = 0,

    /// <summary>
    /// Platform needs app-drawn window shadow.
    /// </summary>
    Shadow = 1,

    /// <summary>
    /// Platform needs app-drawn window border/frame.
    /// </summary>
    Border = 2,

    /// <summary>
    /// Platform needs app-drawn resize grips.
    /// </summary>
    ResizeGrips = 4,
}
