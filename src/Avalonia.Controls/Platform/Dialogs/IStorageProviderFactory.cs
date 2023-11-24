using Avalonia.Metadata;
using Avalonia.Platform.Storage;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Factory allows to register custom storage provider instead of native implementation.
/// </summary>
[Unstable]
public interface IStorageProviderFactory
{
    IStorageProvider CreateProvider(TopLevel topLevel);
}
