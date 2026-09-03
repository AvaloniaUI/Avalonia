using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.X11.Selections;

/// <summary>
/// An object used to read values, converted to the correct format, from an X11 selection (clipboard/drag-and-drop).
/// </summary>
internal abstract class SelectionDataReader<TItem>(
    X11Atoms atoms,
    IntPtr[] textFormatAtoms,
    DataFormat[] dataFormats)
    : IDisposable
    where TItem : class
{
    protected X11Atoms Atoms { get; } = atoms;

    public async Task<TItem[]> CreateItemsAsync()
    {
        List<DataFormat>? nonFileFormats = null;
        var items = new List<TItem>();
        var hasFiles = false;

        foreach (var format in dataFormats)
        {
            if (DataFormat.File.Equals(format))
            {
                if (hasFiles)
                    continue;

                if (await TryGetAsync(format) is IEnumerable<IStorageItem> storageItems)
                {
                    hasFiles = true;

                    foreach (var storageItem in storageItems)
                        items.Add((TItem)(object)PlatformDataTransferItem.Create(DataFormat.File, storageItem));
                }
            }
            else
                (nonFileFormats ??= []).Add(format);
        }

        // Single item containing all formats except for DataFormat.File.
        if (nonFileFormats is not null)
            items.Add(CreateSingleItem(nonFileFormats.ToArray()));

        return items.ToArray();
    }

    public virtual async Task<object?> TryGetAsync(DataFormat format)
    {
        var formatAtom = DataFormatHelper.ToAtom(format, textFormatAtoms, Atoms, dataFormats);
        if (formatAtom == IntPtr.Zero)
            return null;

        using var session = CreateReadSession();
        var result = await session.SendDataRequest(formatAtom, 0).ConfigureAwait(false);
        return ConvertDataResult(result, format, formatAtom);
    }

    protected abstract TItem CreateSingleItem(DataFormat[] nonFileFormats);

    protected abstract SelectionReadSession CreateReadSession();

    private object? ConvertDataResult(SelectionReadSession.GetDataResult? result, DataFormat format, IntPtr formatAtom)
    {
        if (result is null)
            return null;

        if (DataFormat.Text.Equals(format))
        {
            return DataFormatHelper.TryGetStringEncoding(result.TypeAtom, Atoms) is { } textEncoding ?
                textEncoding.GetString(result.AsBytes()) :
                null;
        }

        if (DataFormat.Bitmap.Equals(format))
        {
            using var data = result.AsStream();

            return new Bitmap(data);
        }

        if (DataFormat.File.Equals(format))
        {
            // text/uri-list might not be supported
            return formatAtom != IntPtr.Zero && result.TypeAtom == formatAtom ?
                UriListHelper.Utf8BytesToFileUriList(result.AsBytes()) :
                null;
        }

        if (format is DataFormat<string>)
            return Encoding.UTF8.GetString(result.AsBytes());

        if (format is DataFormat<byte[]>)
            return result.AsBytes();

        return null;
    }

    public abstract void Dispose();
}
