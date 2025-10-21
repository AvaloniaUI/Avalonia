using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represents a platform-specific implementation of the clipboard that can be flushed.
/// </summary>
[PrivateApi]
public interface IFlushableClipboardImpl : IClipboardImpl
{
    /// <inheritdoc cref="IClipboard.FlushAsync"/>
    Task FlushAsync();
}
