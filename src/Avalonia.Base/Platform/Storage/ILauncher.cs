using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Starts the default app associated with the specified file or URI.
/// </summary>
public interface ILauncher
{
    /// <summary>
    /// Starts the default app associated with the URI scheme name for the specified URI.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <returns>True, if launch operation was successful. False, if unsupported or failed.</returns>
    Task<bool> LaunchUriAsync(Uri uri);

    /// <summary>
    /// Starts the default app associated with the specified storage file or folder.
    /// </summary>
    /// <param name="storageItem">The file or folder.</param>
    /// <returns>True, if launch operation was successful. False, if unsupported or failed.</returns>
    Task<bool> LaunchFileAsync(IStorageItem storageItem);
}

internal class NoopLauncher : ILauncher
{
    public Task<bool> LaunchUriAsync(Uri uri) => Task.FromResult(false); 
    public Task<bool> LaunchFileAsync(IStorageItem storageItem) => Task.FromResult(false);
} 

public static class LauncherExtensions
{
    /// <summary>
    /// Starts the default app associated with the specified storage file.
    /// </summary>
    /// <param name="launcher">ILauncher instance.</param>
    /// <param name="fileInfo">The file.</param>
    public static Task<bool> LaunchFileInfoAsync(this ILauncher launcher, FileInfo fileInfo)
    {
        _ = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        if (!fileInfo.Exists)
        {
            return Task.FromResult(false);
        }

        return launcher.LaunchFileAsync(new BclStorageFile(fileInfo));
    }

    /// <summary>
    /// Starts the default app associated with the specified storage directory (folder).
    /// </summary>
    /// <param name="launcher">ILauncher instance.</param>
    /// <param name="directoryInfo">The directory.</param>
    public static Task<bool> LaunchDirectoryInfoAsync(this ILauncher launcher, DirectoryInfo directoryInfo)
    {
        _ = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
        if (!directoryInfo.Exists)
        {
            return Task.FromResult(false);
        }

        return launcher.LaunchFileAsync(new BclStorageFolder(directoryInfo));
    }
}
