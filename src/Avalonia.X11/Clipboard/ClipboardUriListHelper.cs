using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.X11.Clipboard;

internal static class ClipboardUriListHelper
{
    public static IStorageItem[] TryReadFileUriList(Stream source)
    {
        try
        {
            using var reader = new StreamReader(source, Encoding.UTF8);
            var items = new List<IStorageItem>();

            while (reader.ReadLine() is { } line)
            {
                if (Uri.TryCreate(line, UriKind.Absolute, out var uri) &&
                    uri.IsFile &&
                    StorageProviderHelpers.TryCreateBclStorageItem(uri.LocalPath) is { } storageItem)
                {
                    items.Add(storageItem);
                }
            }

            return items.ToArray();
        }
        catch
        {
            return [];
        }
    }

    public static void WriteFileUriList(Stream destination, IEnumerable<IStorageItem> items)
    {
        using var writer = new StreamWriter(destination, Encoding.UTF8);

        writer.NewLine = "\r\n"; // CR+LF is mandatory according to the text/uri-list spec

        foreach (var item in items)
            writer.WriteLine(item.Path.AbsoluteUri);
    }
}
