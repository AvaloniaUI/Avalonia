using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Storage;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Avalonia.Browser;

internal class BrowserDataObject : IDataObject
{
    private readonly JSObject _dataObject;

    public BrowserDataObject(JSObject dataObject)
    {
        _dataObject = dataObject;
    }
    
    public IEnumerable<string> GetDataFormats()
    {
        var types = new HashSet<string>(_dataObject.GetPropertyAsStringArray("types"));
        var dataFormats = new HashSet<string>(types.Count);

        foreach (var type in types)
        {
            if (type.StartsWith("text/", StringComparison.Ordinal))
            {
                dataFormats.Add(DataFormats.Text);
            }
            else if (type.Equals("Files", StringComparison.Ordinal))
            {
                dataFormats.Add(DataFormats.Files);
            }
            dataFormats.Add(type);
        }

        // If drag'n'drop an image from the another web page, if won't add "Files" to the supported types, but only a "text/uri-list".
        // With "text/uri-list" browser can add actual file as well.
        var filesCount = _dataObject.GetPropertyAsJSObject("files")?.GetPropertyAsInt32("count");
        if (filesCount > 0)
        {
            dataFormats.Add(DataFormats.Files);
        }

        return dataFormats;
    }

    public bool Contains(string dataFormat)
    {
        return GetDataFormats().Contains(dataFormat);
    }

    public object? Get(string dataFormat)
    {
        if (dataFormat == DataFormats.Files)
        {
            var files = _dataObject.GetPropertyAsJSObject("files");
            if (files is not null)
            {
                return StorageHelper.FilesToItemsArray(files)
                    .Select(reference => reference.GetPropertyAsString("kind") switch
                    {
                        "directory" => (IStorageItem)new JSStorageFolder(reference),
                        "file" => new JSStorageFile(reference),
                        _ => null
                    })
                    .Where(i => i is not null)
                    .ToArray()!;
            }

            return null;
        }

        if (dataFormat == DataFormats.Text)
        {
            if (_dataObject.CallMethodString("getData", "text/plain") is { Length :> 0 } textData)
            {
                return textData;
            }
        }

        if (_dataObject.CallMethodString("getData", dataFormat) is { Length: > 0 } data)
        {
            return data;
        }

        return null;
    }
}
