using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represents a platform-specific implementation of the clipboard.
/// </summary>
[PrivateApi]
public interface IClipboardImpl
{
    /// <inheritdoc cref="IClipboard.GetDataFormatsAsync"/>
    Task<DataFormat[]> GetDataFormatsAsync();

    /// <inheritdoc cref="IClipboard.TryGetDataAsync"/>
    Task<IDataTransfer?> TryGetDataAsync(IEnumerable<DataFormat> formats);

    /// <inheritdoc cref="IClipboard.SetDataAsync"/>
    Task SetDataAsync(IDataTransfer dataTransfer);

    /// <inheritdoc cref="IClipboard.ClearAsync"/>
    Task ClearAsync();
}
