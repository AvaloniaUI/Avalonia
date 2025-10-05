using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.X11.Clipboard;

internal static class ClipboardUriListHelper
{
    private static readonly Encoding s_utf8NoBomEncoding = new UTF8Encoding(false);

    public static IStorageItem[] Utf8BytesToFileUriList(byte[] utf8Bytes)
    {
        try
        {
            using var stream = new MemoryStream(utf8Bytes);
            using var reader = new StreamReader(stream, s_utf8NoBomEncoding);
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

    public static byte[] FileUriListToUtf8Bytes(IEnumerable<IStorageItem> items)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, s_utf8NoBomEncoding);

        writer.NewLine = "\r\n"; // CR+LF is mandatory according to the text/uri-list spec

        foreach (var item in items)
            writer.WriteLine(item.Path.AbsoluteUri);

        return stream.ToArray();
    }
}
