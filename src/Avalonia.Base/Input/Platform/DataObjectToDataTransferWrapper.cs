using System;
using System.Collections.Generic;
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
    : IDataTransfer
{
    public IDataObject DataObject { get; } = dataObject;

    public IEnumerable<DataFormat> GetFormats()
        => DataObject.GetDataFormats().Select(DataFormats.ToDataFormat).Distinct();

    public IEnumerable<IDataTransferItem> GetItems(IEnumerable<DataFormat>? formats = null)
    {
        DataFormat[]? formatArray = null;

        if (formats is not null)
        {
            formatArray = formats as DataFormat[] ?? formats.ToArray();
            if (formatArray.Length == 0)
                return [];
        }

        return GetItemsCore();

        IEnumerable<IDataTransferItem> GetItemsCore()
        {
            foreach (var formatString in DataObject.GetDataFormats())
            {
                var format = DataFormats.ToDataFormat(formatString);
                if (formatArray is not null && Array.IndexOf(formatArray, format) < 0)
                    continue;

                if (formatString == DataFormats.Files)
                {
                    // This is not ideal as we're reading the filenames ahead of time to generate the appropriate items.
                    // We don't really care about that for this legacy wrapper.
                    if (DataObject.Get(formatString) is IEnumerable<IStorageItem> storageItems)
                    {
                        foreach (var storageItem in storageItems)
                            yield return DataTransferItem.Create(format, storageItem);
                    }
                }
                else if (formatString == DataFormats.FileNames)
                {
                    if (DataObject.Get(formatString) is IEnumerable<string> fileNames)
                    {
                        foreach (var fileName in fileNames)
                        {
                            if (StorageProviderHelpers.TryCreateBclStorageItem(fileName) is { } storageItem)
                                yield return DataTransferItem.Create(format, storageItem);
                        }
                    }
                }
                else
                    yield return DataTransferItem.Create(format, () => DataObject.Get(formatString));
            }
        }
    }

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "IDisposable may be implemented externally.")]
    public void Dispose()
        => (DataObject as IDisposable)?.Dispose();
}
