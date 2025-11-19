using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represents a platform-specific implementation of the clipboard that keeps track of its current owner.
/// </summary>
[PrivateApi]
public interface IOwnedClipboardImpl : IClipboardImpl
{
    /// <summary>
    /// Gets whether the current instance still owns the system clipboard.
    /// </summary>
    Task<bool> IsCurrentOwnerAsync();
}
