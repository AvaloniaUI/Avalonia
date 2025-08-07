using System;

namespace Avalonia.Input.Platform;

/// <summary>
/// Wraps a legacy <see cref="IDataObject"/> into a <see cref="ISyncDataTransferItem"/>.
/// </summary>
[Obsolete]
internal sealed class DataObjectToDataTransferItemWrapper(
    IDataObject dataObject,
    DataFormat[] formats,
    string[] formatStrings)
    : PlatformSyncDataTransferItem
{
    private readonly IDataObject _dataObject = dataObject;
    private readonly DataFormat[] _formats = formats;
    private readonly string[] _formatStrings = formatStrings;

    protected override DataFormat[] ProvideFormats()
        => _formats;

    protected override object? TryGetCore(DataFormat format)
    {
        var index = Array.IndexOf(Formats, format);
        if (index < 0)
            return null;

        var formatString = _formatStrings[index];
        return _dataObject.Get(formatString);
    }
}
