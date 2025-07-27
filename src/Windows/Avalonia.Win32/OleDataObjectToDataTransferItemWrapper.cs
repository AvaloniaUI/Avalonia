using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using MicroCom.Runtime;

namespace Avalonia.Win32;

/// <summary>
/// Wraps a Win32 <see cref="Win32Com.IDataObject"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="oleDataObject">The wrapped OLE data object.</param>
/// <param name="formats">The formats for this item.</param>
internal sealed class OleDataObjectToDataTransferItemWrapper(Win32Com.IDataObject oleDataObject, DataFormat[] formats)
    : IDataTransferItem
{
    private readonly Win32Com.IDataObject _oleDataObject = oleDataObject.CloneReference();
    private readonly DataFormat[] _formats = formats;

    public IEnumerable<DataFormat> GetFormats()
        => _formats;

    public bool Contains(DataFormat format)
        => Array.IndexOf(_formats, format) >= 0;

    public bool ContainsAny(ReadOnlySpan<DataFormat> formats)
        => _formats.AsSpan().IndexOfAny(formats) >= 0;

    public Task<object?> TryGetAsync(DataFormat format)
    {
        if (!Contains(format))
            return Task.FromResult<object?>(null);

        try
        {
            return Task.FromResult(_oleDataObject.TryGet(format));
        }
        catch (Exception ex)
        {
            return Task.FromException<object?>(ex);
        }
    }
}
