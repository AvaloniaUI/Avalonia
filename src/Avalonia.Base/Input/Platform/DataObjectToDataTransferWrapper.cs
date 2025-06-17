using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

// TODO12: remove
/// <summary>
/// Wraps a legacy <see cref="IDataObject"/> into a <see cref="IDataTransfer"/>.
/// </summary>
internal sealed class DataObjectToDataTransferWrapper(IDataObject dataObject)
    : IDataTransfer, IDataTransferItem
{
    public IDataObject DataObject { get; } = dataObject;

    public IEnumerable<DataFormat> GetFormats()
        => DataObject.GetDataFormats().Select(DataFormat.Parse);

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

    public bool Contains(DataFormat format)
        => DataObject.Contains(format.SystemName);

    public object? TryGet(DataFormat format)
    {
        var formatString = format.SystemName;
        return DataObject.Contains(formatString) ? DataObject.Get(formatString) : null;
    }

    public Task<object?> TryGetAsync(DataFormat format)
        => Task.FromResult(TryGet(format));

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "IDataObject may be implemented externally.")]
    public void Dispose()
        => (DataObject as IDisposable)?.Dispose();
}
