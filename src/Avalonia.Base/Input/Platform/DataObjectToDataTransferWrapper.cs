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
internal sealed class DataObjectToDataTransferWrapper(IDataObject dataObject)
    : PlatformDataTransfer
{
    public IDataObject DataObject { get; } = dataObject;

    protected override DataFormat[] ProvideFormats()
        => DataObject.GetDataFormats().Select(DataFormats.ToDataFormat).Distinct().ToArray();

    protected override IDataTransferItem[] ProvideItems()
    {
        var items = new List<IDataTransferItem>();
        var nonFileFormats = new List<DataFormat>();
        var nonFileFormatStrings = new List<string>();

        foreach (var formatString in DataObject.GetDataFormats())
        {
            var format = DataFormats.ToDataFormat(formatString);

            if (formatString == DataFormats.Files)
            {
                // This is not ideal as we're reading the filenames ahead of time to generate the appropriate items.
                // We don't really care about that for this legacy wrapper.
                if (DataObject.Get(formatString) is IEnumerable<IStorageItem> storageItems)
                {
                    foreach (var storageItem in storageItems)
                        items.Add(DataTransferItem.Create(format, storageItem));
                }
            }
            else if (formatString == DataFormats.FileNames)
            {
                if (DataObject.Get(formatString) is IEnumerable<string> fileNames)
                {
                    foreach (var fileName in fileNames)
                    {
                        if (StorageProviderHelpers.TryCreateBclStorageItem(fileName) is { } storageItem)
                            items.Add(DataTransferItem.Create(format, storageItem));
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
