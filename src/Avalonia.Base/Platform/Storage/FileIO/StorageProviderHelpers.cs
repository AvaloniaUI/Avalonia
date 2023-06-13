using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

internal static class StorageProviderHelpers
{
    public static IStorageItem? TryCreateBclStorageItem(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var directory = new DirectoryInfo(path);
            if (directory.Exists)
            {
                return new BclStorageFolder(directory);
            }

            var file = new FileInfo(path);
            if (file.Exists)
            {
                return new BclStorageFile(file);
            }
        }

        return null;
    }

    public static Uri FilePathToUri(string path)
    {
        var uriPath = new StringBuilder(path)
            .Replace("%", $"%{(int)'%':X2}")
            .Replace("[", $"%{(int)'[':X2}")
            .Replace("]", $"%{(int)']':X2}")
            .ToString();

        return new UriBuilder("file", string.Empty) { Path = uriPath }.Uri;
    }
    
    public static bool TryFilePathToUri(string path, [NotNullWhen(true)] out Uri? uri)
    {
        try
        {
            uri = FilePathToUri(path);
            return true;
        }
        catch
        {
            uri = null;
            return false;
        }
    }
    
    [return: NotNullIfNotNull(nameof(path))]
    public static string? NameWithExtension(string? path, string? defaultExtension, FilePickerFileType? filter)
    {
        var name = Path.GetFileName(path);
        if (name != null && !Path.HasExtension(name))
        {
            if (filter?.Patterns?.Count > 0)
            {
                if (defaultExtension != null
                    && filter.Patterns.Contains(defaultExtension))
                {
                    return Path.ChangeExtension(path, defaultExtension.TrimStart('.'));
                }

                var ext = filter.Patterns.FirstOrDefault(x => x != "*.*");
                ext = ext?.Split(new[] { "*." }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (ext != null)
                {
                    return Path.ChangeExtension(path, ext);
                }
            }

            if (defaultExtension != null)
            {
                return Path.ChangeExtension(path, defaultExtension);
            }
        }

        return path;
    }
}
