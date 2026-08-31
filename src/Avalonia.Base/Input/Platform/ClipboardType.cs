namespace Avalonia.Input.Platform;

/// <summary>
/// Identifies a clipboard provided by the platform.
/// </summary>
public enum ClipboardType
{
    /// <summary>
    /// The default system clipboard populated by explicit copy/cut operations.
    /// </summary>
    Default,

    /// <summary>
    /// The primary selection, an implicit clipboard populated by selecting text
    /// on platforms supporting it (X11/Wayland).
    /// </summary>
    PrimarySelection
}
