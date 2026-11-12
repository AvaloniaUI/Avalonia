namespace Avalonia.Input.Platform;

/// <summary>
/// Simple <see cref="IPlatformClipboardManagerImpl"/> implementation holding pre-created clipboard instances.
/// </summary>
internal sealed class PlatformClipboardManager(IClipboard? clipboard, IClipboard? primarySelection)
    : IPlatformClipboardManagerImpl
{
    public IClipboard? TryGetClipboard(ClipboardType type) => type switch
    {
        ClipboardType.Default => clipboard,
        ClipboardType.PrimarySelection => primarySelection,
        _ => null
    };
}
