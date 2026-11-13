using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Provides access to the clipboards available on the platform.
/// </summary>
[PrivateApi]
public interface IPlatformClipboardManagerImpl
{
    /// <summary>
    /// Gets the clipboard of the specified type, or null if the platform doesn't provide it.
    /// </summary>
    IClipboard? TryGetClipboard(ClipboardType type);
}
