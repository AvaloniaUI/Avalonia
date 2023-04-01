using System.Threading.Tasks;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Group of public extensions for <see cref="IStorageProvider"/> class. 
/// </summary>
public static class StorageProviderExtensions
{
    /// <inheritdoc cref="IStorageProvider.TryGetFileFromPathAsync"/>
    public static Task<IStorageFile?> TryGetFileFromPathAsync(this IStorageProvider provider, string filePath)
    {
        // We can avoid double escaping of the path by checking for BclStorageProvider.
        if (provider is BclStorageProvider)
        {
            return Task.FromResult(StorageProviderHelpers.TryCreateBclStorageItem(filePath) as IStorageFile);
        }

        if (StorageProviderHelpers.TryFilePathToUri(filePath, out var uri))
        {
            return provider.TryGetFileFromPathAsync(uri);
        }

        return Task.FromResult<IStorageFile?>(null);
    }

    /// <inheritdoc cref="IStorageProvider.TryGetFolderFromPathAsync"/>
    public static Task<IStorageFolder?> TryGetFolderFromPathAsync(this IStorageProvider provider, string folderPath)
    {
        // We can avoid double escaping of the path by checking for BclStorageProvider.
        if (provider is BclStorageProvider)
        {
            return Task.FromResult(StorageProviderHelpers.TryCreateBclStorageItem(folderPath) as IStorageFolder);
        }

        if (StorageProviderHelpers.TryFilePathToUri(folderPath, out var uri))
        {
            return provider.TryGetFolderFromPathAsync(uri);
        }

        return Task.FromResult<IStorageFolder?>(null);
    }

    /// <summary>
    /// Gets the local file system path of the item as a string.
    /// </summary>
    /// <param name="item">Storage folder or file.</param>
    /// <returns>Full local path to the folder or file if possible, otherwise null.</returns>
    /// <remarks>
    /// Android platform usually uses "content:" virtual file paths
    /// and Browser platform has isolated access without full paths,
    /// so on these platforms this method will return null.
    /// </remarks>
    public static string? TryGetLocalPath(this IStorageItem item)
    {
        // We can avoid double escaping of the path by checking for BclStorageFolder.
        // Ideally, `folder.Path.LocalPath` should also work, as that's only available way for the users.
        if (item is BclStorageFolder storageFolder)
        {
            return storageFolder.DirectoryInfo.FullName;
        }
        if (item is BclStorageFile storageFile)
        {
            return storageFile.FileInfo.FullName;
        }

        if (item.Path is { IsAbsoluteUri: true, Scheme: "file" } absolutePath)
        {
            return absolutePath.LocalPath;
        }

        // android "content:", browser and ios relative links go here. 
        return null;
    }
}
