using Avalonia.Metadata;

namespace Avalonia.Controls.Platform;

[Unstable]
public interface INativePopupImpl
{
    /// <summary>
    /// A Boolean value that determines whether the native NSWindow can become key window.
    /// </summary>
    bool CanBecomeKeyWindow { get; set; }
}
