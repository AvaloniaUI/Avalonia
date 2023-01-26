using System.Threading.Tasks;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Group of public extensions for <see cref="IStorageProvider"/> class. 
/// </summary>
public static class StorageProviderExtensions
{
    /// <inheritdoc cref="IStorageProvider.TryGetFileFromPath"/>
    public static Task<IStorageFile?> TryGetFileFromPath(this IStorageProvider provider, string filePath)
    {
        return provider.TryGetFileFromPath(StorageProviderHelpers.FilePathToUri(filePath));
    }

    /// <inheritdoc cref="IStorageProvider.TryGetFolderFromPath"/>
    public static Task<IStorageFolder?> TryGetFolderFromPath(this IStorageProvider provider, string folderPath)
    {
        return provider.TryGetFolderFromPath(StorageProviderHelpers.FilePathToUri(folderPath));
    }

    internal static string? TryGetFullPath(this IStorageFolder folder)
    {
        // We can avoid double escaping of the path by checking for BclStorageFolder.
        // Ideally, `folder.Path.LocalPath` should also work, as that's only available way for the users.
        if (folder is BclStorageFolder storageFolder)
        {
            return storageFolder.DirectoryInfo.FullName;
        }

        if (folder.Path is { IsAbsoluteUri: true, Scheme: "file" } absolutePath)
        {
            return absolutePath.LocalPath;
        }

        // android "content:", browser and ios relative links go here. 
        return null;
    }
    
    internal static string? TryGetFullPath(this IStorageFile file)
    {
        if (file is BclStorageFile storageFolder)
        {
            return storageFolder.FileInfo.FullName;
        }

        if (file.Path is { IsAbsoluteUri: true, Scheme: "file" } absolutePath)
        {
            return absolutePath.LocalPath;
        }

        return null;
    }
}
