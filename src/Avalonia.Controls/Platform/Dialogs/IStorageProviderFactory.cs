#nullable enable
using Avalonia.Platform.Storage;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Factory allows to register custom storage provider instead of native implementation.
/// </summary>
public interface IStorageProviderFactory
{
    IStorageProvider CreateProvider(TopLevel topLevel);
}
