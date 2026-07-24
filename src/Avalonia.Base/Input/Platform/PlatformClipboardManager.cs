namespace Avalonia.Input.Platform;

/// <summary>
/// Simple <see cref="IPlatformClipboardManagerImpl"/> implementation holding pre-created clipboard instances.
/// </summary>
internal sealed class PlatformClipboardManager(IClipboard? clipboard, IClipboard? primarySelection)
    : IPlatformClipboardManagerImpl
{
    public IClipboard? Clipboard { get; } = clipboard;

    public IClipboard? PrimarySelection { get; } = primarySelection;
}
