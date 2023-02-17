using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Compatibility;

namespace Avalonia.Platform.Storage.FileIO;

internal abstract class BclStorageProvider : IStorageProvider
{
    public abstract bool CanOpen { get; }
    public abstract Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

    public abstract bool CanSave { get; }
    public abstract Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);

    public abstract bool CanPickFolder { get; }
    public abstract Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);

    public virtual Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        var file = new FileInfo(bookmark);
        return file.Exists
            ? Task.FromResult<IStorageBookmarkFile?>(new BclStorageFile(file))
            : Task.FromResult<IStorageBookmarkFile?>(null);
    }

    public virtual Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        var folder = new DirectoryInfo(bookmark);
        return folder.Exists
            ? Task.FromResult<IStorageBookmarkFolder?>(new BclStorageFolder(folder))
            : Task.FromResult<IStorageBookmarkFolder?>(null);
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
            return Task.FromResult<IStorageFolder?>(null);
        }

        var directory = new DirectoryInfo(folderPath);
        if (!directory.Exists)
        {
            return Task.FromResult<IStorageFolder?>(null);
        }
        
        return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));

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
                SHGetKnownFolderPath(s_folderDownloads, 0, IntPtr.Zero);
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
    
    private static readonly Guid s_folderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, int flags, IntPtr token);
}
