using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Input.Platform;

#pragma warning disable CS0618 // Type or member is obsolete: usages of IDataObject and DataFormats

// TODO12: remove
/// <summary>
/// Wraps a legacy <see cref="IDataObject"/> into a <see cref="IDataTransfer"/>.
/// </summary>
[Obsolete]
internal sealed class DataObjectToDataTransferWrapper(IDataObject dataObject)
    : PlatformDataTransfer
{
    public IDataObject DataObject { get; } = dataObject;

    protected override DataFormat[] ProvideFormats()
        => DataObject.GetDataFormats().Select(DataFormats.ToDataFormat).Distinct().ToArray();

    protected override PlatformDataTransferItem[] ProvideItems()
    {
        var items = new List<PlatformDataTransferItem>();
        var nonFileFormats = new List<DataFormat>();
        var nonFileFormatStrings = new List<string>();
        var hasFiles = false;

        foreach (var formatString in DataObject.GetDataFormats())
        {
            var format = DataFormats.ToDataFormat(formatString);

            if (formatString == DataFormats.Files)
            {
                if (hasFiles)
                    continue;

                // This is not ideal as we're reading the filenames ahead of time to generate the appropriate items.
                // We don't really care about that for this legacy wrapper.
                if (DataObject.Get(formatString) is IEnumerable<IStorageItem> storageItems)
                {
                    hasFiles = true;

                    foreach (var storageItem in storageItems)
                        items.Add(PlatformDataTransferItem.Create(DataFormat.File, storageItem));
                }
            }
            else if (formatString == DataFormats.FileNames)
            {
                if (hasFiles)
                    continue;

                if (DataObject.Get(formatString) is IEnumerable<string> fileNames)
                {
                    hasFiles = true;

                    foreach (var fileName in fileNames)
                    {
                        if (StorageProviderHelpers.TryCreateBclStorageItem(fileName) is { } storageItem)
                            items.Add(PlatformDataTransferItem.Create(DataFormat.File, storageItem));
                    }
                }
            }
            else
            {
                nonFileFormats.Add(format);
                nonFileFormatStrings.Add(formatString);
            }
        }

        if (nonFileFormats.Count > 0)
        {
            Debug.Assert(nonFileFormats.Count == nonFileFormatStrings.Count);

            // Single item containing all formats except for DataFormat.File.
            items.Add(new DataObjectToDataTransferItemWrapper(
                DataObject,
                nonFileFormats.ToArray(),
                nonFileFormatStrings.ToArray()));
        }

        return items.ToArray();
    }

    [SuppressMessage(
        "ReSharper",
        "SuspiciousTypeConversion.Global",
        Justification = "IDisposable may be implemented externally by the IDataObject instance.")]
    public override void Dispose()
        => (DataObject as IDisposable)?.Dispose();
}
