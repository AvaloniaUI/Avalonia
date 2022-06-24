using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Platform.Storage;

namespace Avalonia.Controls.Platform;

[Unstable]
public interface ITopLevelImplWithStorageProvider : ITopLevelImpl
{
    public IStorageProvider StorageProvider { get; }
}
