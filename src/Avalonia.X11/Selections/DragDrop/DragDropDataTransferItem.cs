using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Selections.DragDrop;

/// <summary>
/// Implementation of <see cref="IAsyncDataTransferItem"/> for Xdnd.
/// </summary>
/// <param name="reader">The object used to read values.</param>
/// <param name="formats">The formats.</param>
internal sealed class DragDropDataTransferItem(DragDropDataReader reader, DataFormat[] formats)
    : PlatformDataTransferItem
{
    private Dictionary<DataFormat, object?>? _cachedValues;

    protected override DataFormat[] ProvideFormats()
        => formats;

    protected override object? TryGetRawCore(DataFormat format)
    {
        if (_cachedValues is null || !_cachedValues.TryGetValue(format, out var value))
        {
            value = reader.TryGet(format);
            _cachedValues ??= [];
            _cachedValues[format] = value;
        }

        return value;
    }
}
