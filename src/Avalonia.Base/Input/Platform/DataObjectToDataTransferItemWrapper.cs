using System;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Wraps a legacy <see cref="IDataObject"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
[Obsolete]
internal sealed class DataObjectToDataTransferItemWrapper(
    IDataObject dataObject,
    DataFormat[] formats,
    string[] formatStrings)
    : PlatformDataTransferItem
{
    private readonly IDataObject _dataObject = dataObject;
    private readonly DataFormat[] _formats = formats;
    private readonly string[] _formatStrings = formatStrings;

    protected override DataFormat[] ProvideFormats()
        => _formats;

    protected override Task<object?> TryGetAsyncCore(DataFormat format)
    {
        try
        {
            return Task.FromResult(TryGet(format));
        }
        catch (Exception ex)
        {
            return Task.FromException<object?>(ex);
        }
    }

    private object? TryGet(DataFormat format)
    {
        var index = Array.IndexOf(Formats, format);
        if (index < 0)
            return null;

        var formatString = _formatStrings[index];
        return _dataObject.Get(formatString);
    }
}
