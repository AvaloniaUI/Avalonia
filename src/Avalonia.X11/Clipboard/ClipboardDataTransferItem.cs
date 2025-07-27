using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Clipboard;

/// <summary>
/// Implementation of <see cref="IDataTransferItem"/> for the X11 clipboard.
/// </summary>
/// <param name="reader">The object used to read values.</param>
/// <param name="formats">The formats.</param>
internal sealed class ClipboardDataTransferItem(ClipboardDataReader reader, DataFormat[] formats)
    : IDataTransferItem
{
    private readonly ClipboardDataReader _reader = reader;
    private readonly DataFormat[] _formats = formats;

    public IEnumerable<DataFormat> GetFormats()
        => _formats;

    public bool Contains(DataFormat format)
        => Array.IndexOf(_formats, format) >= 0;

    public Task<object?> TryGetAsync(DataFormat format)
        => Contains(format) ? _reader.TryGetAsync(format) : Task.FromResult<object?>(null);
}
