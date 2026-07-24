using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Provides access to the clipboards available on the platform.
/// </summary>
[PrivateApi]
public interface IPlatformClipboardManagerImpl
{
    /// <summary>
    /// Gets the system clipboard, if available.
    /// </summary>
    IClipboard? Clipboard { get; }

    /// <summary>
    /// Gets the primary selection (implicit clipboard populated by selecting text on X11/Wayland), if available.
    /// </summary>
    IClipboard? PrimarySelection { get; }
}
