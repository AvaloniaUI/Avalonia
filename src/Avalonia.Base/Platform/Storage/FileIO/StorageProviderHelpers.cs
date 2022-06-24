using System;
using System.IO;
using System.Linq;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage.FileIO;

[Unstable]
public static class StorageProviderHelpers
{
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
