using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

[NotClientImplementable]
public interface IStorageBookmarkItem : IStorageItem
{
    Task ReleaseBookmarkAsync();
}

[NotClientImplementable]
public interface IStorageBookmarkFile : IStorageFile, IStorageBookmarkItem
{
}

[NotClientImplementable]
public interface IStorageBookmarkFolder : IStorageFolder, IStorageBookmarkItem
{
}
