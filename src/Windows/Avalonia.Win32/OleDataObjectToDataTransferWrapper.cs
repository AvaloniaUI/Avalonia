using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using MicroCom.Runtime;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using IEnumFORMATETC = Avalonia.Win32.Win32Com.IEnumFORMATETC;

namespace Avalonia.Win32;

/// <summary>
/// Wraps a Win32 <see cref="Win32Com.IDataObject"/> into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="oleDataObject">The wrapped OLE data object.</param>
internal sealed class OleDataObjectToDataTransferWrapper(Win32Com.IDataObject oleDataObject)
    : IDataTransfer
{
    private readonly Win32Com.IDataObject _oleDataObject = oleDataObject.CloneReference();
    private DataFormat[]? _formats;
    private IDataTransferItem[]? _items;

    public DataFormat[] Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
            {
                if (_oleDataObject.EnumFormatEtc((int)DATADIR.DATADIR_GET) is not { } enumFormat)
                    return [];

                enumFormat.Reset();

                var formats = new List<DataFormat>();

                while (Next(enumFormat) is { } format)
                    formats.Add(format);

                return formats.ToArray();

                static unsafe DataFormat? Next(IEnumFORMATETC enumFormat)
                {
                    var fetched = 1u;
                    FORMATETC formatEtc;

                    var result = enumFormat.Next(1, &formatEtc, &fetched);
                    if (result != 0 || fetched == 0)
                        return null;

                    if (formatEtc.ptd != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(formatEtc.ptd);

                    return ClipboardFormatRegistry.GetFormatById(formatEtc.cfFormat);
                }
            }
        }
    }

    public IDataTransferItem[] Items
    {
        get
        {
            return _items ??= GetItemsCore();

            IDataTransferItem[] GetItemsCore()
            {
                List<DataFormat>? nonFileFormats = null;
                var items = new List<IDataTransferItem>();

                foreach (var format in Formats)
                {
                    if (DataFormat.File.Equals(format))
                    {
                        // This is not ideal as we're reading the filenames ahead of time to generate the appropriate items.
                        // However, it's very likely that we're filtering on formats, so files are requested by the caller.
                        // If this isn't the case, this still isn't a heavy operation.
                        if (_oleDataObject.TryGet(format) is IEnumerable<IStorageItem> storageItems)
                        {
                            foreach (var storageItem in storageItems)
                                items.Add(DataTransferItem.Create(format, storageItem));
                        }
                    }
                    else
                        (nonFileFormats ??= new()).Add(format);
                }

                // Single item containing all formats except for File.
                if (nonFileFormats is not null)
                    items.Add(new OleDataObjectToDataTransferItemWrapper(_oleDataObject, Formats));

                return items.ToArray();
            }
        }
    }

    public IEnumerable<DataFormat> GetFormats()
        => Formats;

    public IEnumerable<IDataTransferItem> GetItems(IEnumerable<DataFormat>? formats)
    {
        if (formats is null)
            return Items;

        var formatArray = formats as DataFormat[] ?? formats.ToArray();
        if (formatArray.Length == 0)
            return [];

        return FilterItems();

        IEnumerable<IDataTransferItem> FilterItems()
        {
            foreach (var item in Items)
            {
                foreach (var format in formatArray)
                {
                    if (item.Contains(format))
                    {
                        yield return item;
                        break;
                    }
                }
            }
        }
    }

    public bool Contains(DataFormat format)
        => Array.IndexOf(Formats, format) >= 0;

    public void Dispose()
        => _oleDataObject.Dispose();
}
