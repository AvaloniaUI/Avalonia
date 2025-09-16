using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

internal static class StorageProviderHelpers
{
    public static BclStorageItem? TryCreateBclStorageItem(string path)
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

    public static string? TryGetPathFromFileUri(Uri? uri)
    {
        // android "content:", browser and ios relative links are ignored.
        return uri is { IsAbsoluteUri: true, Scheme: "file" } ? uri.LocalPath : null;
    }

    public static Uri UriFromFilePath(string path, bool isDirectory)
    {
        var uriPath = new StringBuilder(path)
            .Replace("%", $"%{(int)'%':X2}")
            .Replace("[", $"%{(int)'[':X2}")
            .Replace("]", $"%{(int)']':X2}");

        if (!path.EndsWith('/') && isDirectory)
        {
            uriPath.Append('/');
        }

        return new UriBuilder("file", string.Empty) { Path = uriPath.ToString() }.Uri;
    }

    public static Uri? TryGetUriFromFilePath(string path, bool isDirectory)
    {
        try
        {
            return UriFromFilePath(path, isDirectory);
        }
        catch
        {
            return null;
        }
    }

    public static FilePickerFileType? TryGetFileTypeFromKnownList(
        string? mimeType, string? extension, IReadOnlyList<FilePickerFileType>? choices)
    {
        if (choices is null || choices.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(mimeType))
        {
            foreach (var choice in choices)
            {
                if (choice.MimeTypes?.Contains(mimeType) == true)
                    return choice;
            }
        }

        if (!string.IsNullOrEmpty(extension))
        {
            var extensionPattern = "*." + extension.TrimStart('.');

            foreach (var choice in choices)
            {
                if (choice.Patterns?.Contains(extensionPattern) == true)
                    return choice;
            }
        }

        return choices.Contains(FilePickerFileTypes.All) ? FilePickerFileTypes.All : null;
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
