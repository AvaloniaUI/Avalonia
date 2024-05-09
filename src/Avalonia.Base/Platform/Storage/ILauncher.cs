using System;
using System.IO;
using System.Threading.Tasks;
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

    /// <summary>
    /// Raised before a URI is launched.
    /// </summary>
    /// <remarks>
    /// The event handler receives an argument of type <see cref="LauncherEventArgs{Uri}"/> containing data related to this event.
    /// This event can be used to intercept the file launch operation. Setting the <see cref="LauncherEventArgs{Uri}.Handled"/> property to true prevents the default launch operation.
    /// </remarks>
    event Action<LauncherEventArgs<Uri>>? UriLaunching;

    /// <summary>
    /// Raised before a file launch operation is performed.
    /// </summary>
    /// <remarks>
    /// The event handler receives an argument of type <see cref="LauncherEventArgs{IStorageItem}"/> containing data related to this event.
    /// This event can be used to intercept the file launch operation. Setting the <see cref="LauncherEventArgs{IStorageItem}.Handled"/> property to true prevents the default launch operation.
    /// </remarks>
    event Action<LauncherEventArgs<IStorageItem>>? FileLaunching;
}

/// <summary>
/// Provides context for UriLaunching and FileLaunching events in <see cref="ILauncher"/>.
/// </summary>
/// <typeparam name="T">The type of the argument (either <see cref="Uri"/> or <see cref="IStorageItem"/>) associated with the event.</typeparam>
public class LauncherEventArgs<T> : EventArgs
{
    internal LauncherEventArgs(T argument)
    {
        Argument = argument;
    }
    
    /// <summary>
    /// Gets the argument associated with the event (<see cref="Uri"/> or <see cref="IStorageItem"/>).
    /// </summary>
    public T Argument { get; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the event was handled.
    /// </summary>
    public bool Handled { get; set; } = false;
}

/// <summary>
/// <see cref="ILauncher"/> abstract base implementation
/// </summary>
public abstract class Launcher : ILauncher
{
    /// <inheritdoc />
    public event Action<LauncherEventArgs<Uri>>? UriLaunching;

    /// <inheritdoc />
    public event Action<LauncherEventArgs<IStorageItem>>? FileLaunching;

    /// <inheritdoc />
    public Task<bool> LaunchUriAsync(Uri uri)
    {
        var uriArgs = new LauncherEventArgs<Uri>(uri);
        UriLaunching?.Invoke(uriArgs);

        if (uriArgs.Handled)
            return Task.FromResult(true);

        return LaunchUriAsyncImpl(uriArgs.Argument);
    }

    /// <inheritdoc />
    public Task<bool> LaunchFileAsync(IStorageItem storageItem)

    {
        var fileArgs = new LauncherEventArgs<IStorageItem>(storageItem);
        FileLaunching?.Invoke(fileArgs);

        if (fileArgs.Handled)
            return Task.FromResult(true);

        return LaunchFileAsyncImpl(fileArgs.Argument);
    }

    /// <summary>
    /// Platform implementation to launch the specified URI (with the associated default app via the URI scheme name).
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <returns>True, if launch operation was successful. False, if unsupported or failed.</returns>
    protected abstract Task<bool> LaunchUriAsyncImpl(Uri uri);

    /// <summary>
    /// Platform implementation to launch the specified storage file or folder (with the associated default app).
    /// </summary>
    /// <param name="storageItem">The file or folder.</param>
    /// <returns>True, if launch operation was successful. False, if unsupported or failed.</returns>
    protected abstract Task<bool> LaunchFileAsyncImpl(IStorageItem storageItem);
}

internal class NoopLauncher : Launcher
{
    protected override Task<bool> LaunchUriAsyncImpl(Uri uri) => Task.FromResult(false);

    protected override Task<bool> LaunchFileAsyncImpl(IStorageItem storageItem) => Task.FromResult(false);
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
