using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

internal static class StorageProviderHelpers
{
    public static Uri FilePathToUri(string path)
    {
        var uriPath = new StringBuilder(path)
            .Replace("%", $"%{(int)'%':X2}")
            .Replace("[", $"%{(int)'[':X2}")
            .Replace("]", $"%{(int)']':X2}")
            .ToString();

        return Path.IsPathRooted(path) ?
            new UriBuilder("file", string.Empty) { Path = uriPath }.Uri :
            new Uri(uriPath, UriKind.Relative);
    }
    
    public static string NameWithExtension(string path, string? defaultExtension, FilePickerFileType? filter)
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
