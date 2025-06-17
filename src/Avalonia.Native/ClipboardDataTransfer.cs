#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native;

internal sealed class ClipboardDataTransfer(IAvnClipboard clipboard)
    : IDataTransfer, IDataTransferItem
{
    private ClipboardImpl? _clipboard = new(clipboard);
    private DataFormat[]? _formats;

    private ClipboardImpl Clipboard
        => _clipboard ?? throw new ObjectDisposedException(nameof(ClipboardDataTransfer));

    private DataFormat[] Formats
        => _formats ??= Clipboard.GetFormats();

    public IEnumerable<IDataTransferItem> GetItems(IEnumerable<DataFormat>? formats = null)
    {
        if (formats is null)
            return [this];

        var formatArray = formats as DataFormat[] ?? formats.ToArray();
        if (formatArray.Length > 0)
        {
            foreach (var format in GetFormats())
            {
                if (Array.IndexOf(formatArray, format) >= 0)
                    return [this];
            }
        }

        return [];
    }

    public IEnumerable<DataFormat> GetFormats()
        => Formats;

    public bool Contains(DataFormat format)
        => Array.IndexOf(Formats, format) >= 0;

    public object? TryGet(DataFormat format)
        => Clipboard.TryGetData(format);

    Task<object?> IDataTransferItem.TryGetAsync(DataFormat format)
        => Task.FromResult(TryGet(format));

    public void Dispose()
    {
        _clipboard?.Dispose();
        _clipboard = null;
    }
}
