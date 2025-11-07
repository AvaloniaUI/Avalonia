using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Compatibility;
using Avalonia.Logging;

namespace Avalonia.Platform.Storage.FileIO;

internal abstract class BclStorageProvider : IStorageProvider
{
    public abstract bool CanOpen { get; }
    public abstract Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

    public abstract bool CanSave { get; }
    public abstract Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);
    public abstract Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options);

    public abstract bool CanPickFolder { get; }
    public abstract Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);

    public virtual Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        return Task.FromResult(OpenBookmark(bookmark) as IStorageBookmarkFile);
    }

    public virtual Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        return Task.FromResult(OpenBookmark(bookmark) as IStorageBookmarkFolder);
    }

    public virtual Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
    {
        if (filePath.IsAbsoluteUri)
        {
            var file = new FileInfo(filePath.LocalPath);
            if (file.Exists)
            {
                return Task.FromResult<IStorageFile?>(new BclStorageFile(file));
            }
        }

        return Task.FromResult<IStorageFile?>(null);
    }

    public virtual Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        if (folderPath.IsAbsoluteUri)
        {
            var directory = new DirectoryInfo(folderPath.LocalPath);
            if (directory.Exists)
            {
                return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));
            }
        }

        return Task.FromResult<IStorageFolder?>(null);
    }

    public virtual Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        if (TryGetWellKnownFolderCore(wellKnownFolder) is { } directoryInfo)
        {
            return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directoryInfo));
        }

        return Task.FromResult<IStorageFolder?>(null);
    }

    internal static DirectoryInfo? TryGetWellKnownFolderCore(WellKnownFolder wellKnownFolder)
    {
        // Note, this BCL API returns different values depending on the .NET version.
        // We should also document it. 
        // https://github.com/dotnet/docs/issues/31423
        // For pre-breaking change values see table:
        // https://johnkoerner.com/csharp/special-folder-values-on-windows-versus-mac/
        var folderPath = wellKnownFolder switch
        {
            WellKnownFolder.Desktop => GetFromSpecialFolder(Environment.SpecialFolder.Desktop),
            WellKnownFolder.Documents => GetFromSpecialFolder(Environment.SpecialFolder.MyDocuments),
            WellKnownFolder.Downloads => GetDownloadsWellKnownFolder(),
            WellKnownFolder.Music => GetFromSpecialFolder(Environment.SpecialFolder.MyMusic),
            WellKnownFolder.Pictures => GetFromSpecialFolder(Environment.SpecialFolder.MyPictures),
            WellKnownFolder.Videos => GetFromSpecialFolder(Environment.SpecialFolder.MyVideos),
            _ => throw new ArgumentOutOfRangeException(nameof(wellKnownFolder), wellKnownFolder, null)
        };

        if (folderPath is null)
        {
            return null;
        }

        var directory = new DirectoryInfo(folderPath);
        if (!directory.Exists)
        {
            return null;
        }

        return directory;

        string GetFromSpecialFolder(Environment.SpecialFolder folder) =>
            Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
    }

    // TODO, replace with https://github.com/dotnet/runtime/issues/70484 when implemented.
    // Normally we want to avoid platform specific code in the Avalonia.Base assembly.
    protected static string? GetDownloadsWellKnownFolder()
    {
        if (OperatingSystemEx.IsWindows())
        {
            return Environment.OSVersion.Version.Major < 6 ? null :
                TryGetWindowsKnownFolder(s_folderDownloads);
        }

        if (OperatingSystemEx.IsLinux())
        {
            var envDir = Environment.GetEnvironmentVariable("XDG_DOWNLOAD_DIR");
            if (envDir != null && Directory.Exists(envDir))
            {
                return envDir;
            }
        }

        if (OperatingSystemEx.IsLinux() || OperatingSystemEx.IsMacOS())
        {
            return "~/Downloads";
        }

        return null;
    }

    private IStorageBookmarkItem? OpenBookmark(string bookmark)
    {
        try
        {
            if (StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out var localPath))
            {
                return StorageProviderHelpers.TryCreateBclStorageItem(localPath);   
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Information, LogArea.Platform)?
                .Log(this, "Unable to read file bookmark: {Exception}", ex);
            return null;
        }
    }

    private static unsafe string? TryGetWindowsKnownFolder(Guid guid)
    {
        char* path = null;
        string? result = null;

        var hr = SHGetKnownFolderPath(&guid, 0, null, &path);
        if (hr == 0)
        {
            result = Marshal.PtrToStringUni((IntPtr)path);
        }

        if (path != null)
        {
            Marshal.FreeCoTaskMem((IntPtr)path);
        }

        return result;
    }

    private static readonly Guid s_folderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static unsafe extern int SHGetKnownFolderPath(Guid* rfid, uint dwFlags, void* hToken, char** ppszPath);
}
