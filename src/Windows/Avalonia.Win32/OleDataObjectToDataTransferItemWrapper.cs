using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Win32;

/// <summary>
/// Wraps a Win32 <see cref="Win32Com.IDataObject"/> into a <see cref="ISyncDataTransferItem"/>.
/// </summary>
/// <param name="oleDataObject">The wrapped OLE data object.</param>
/// <param name="formats">The formats for this item.</param>
internal sealed class OleDataObjectToDataTransferItemWrapper(Win32Com.IDataObject oleDataObject, DataFormat[] formats)
    : PlatformSyncDataTransferItem
{
    private readonly Win32Com.IDataObject _oleDataObject = oleDataObject;
    private readonly DataFormat[] _formats = formats;

    protected override DataFormat[] ProvideFormats()
        => _formats;

    protected override object? TryGetCore(DataFormat format)
        => _oleDataObject.TryGet(format);
}
