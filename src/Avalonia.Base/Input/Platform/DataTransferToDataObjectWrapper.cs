using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Input.Platform;

/// <summary>
/// Wraps a <see cref="IDataTransferItem"/> into a legacy <see cref="IDataObject"/>.
/// </summary>
internal sealed class DataTransferToDataObjectWrapper(IDataTransfer dataTransfer) : IDataObject
{
    public IDataTransfer DataTransfer { get; } = dataTransfer;

    public IEnumerable<string> GetDataFormats()
        => DataTransfer.GetFormats().Select(format => format.SystemName);

    public bool Contains(string dataFormat)
        => DataTransfer.Contains(DataFormat.Parse(dataFormat));

    public object? Get(string dataFormat)
        => DataTransfer.TryGetValueAsync<object?>(DataFormat.Parse(dataFormat)).GetAwaiter().GetResult();
}
