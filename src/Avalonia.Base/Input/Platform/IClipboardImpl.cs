using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represents a platform-specific implementation of the clipboard.
/// </summary>
[PrivateApi]
public interface IClipboardImpl
{
    /// <inheritdoc cref="IClipboard.TryGetDataAsync"/>
    Task<IAsyncDataTransfer?> TryGetDataAsync();

    /// <inheritdoc cref="IClipboard.SetDataAsync"/>
    Task SetDataAsync(IAsyncDataTransfer dataTransfer);

    /// <inheritdoc cref="IClipboard.ClearAsync"/>
    Task ClearAsync();
}
