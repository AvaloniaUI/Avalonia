using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

#pragma warning disable CS0618 // Type or member is obsolete: usages of IDataObject and DataFormats

/// <summary>
/// Wraps a <see cref="IDataTransferItem"/> into a legacy <see cref="IDataObject"/>.
/// </summary>
internal sealed class DataTransferToDataObjectWrapper(IDataTransfer dataTransfer) : IDataObject
{
    public IDataTransfer DataTransfer { get; } = dataTransfer;

    public IEnumerable<string> GetDataFormats()
        => DataTransfer.GetFormats().Select(DataFormats.ToString);

    public bool Contains(string dataFormat)
        => DataTransfer.Contains(DataFormats.ToDataFormat(dataFormat));

    public object? Get(string dataFormat)
    {
        if (dataFormat == DataFormats.Text)
            return DataTransfer.TryGetTextAsync().GetAwaiter().GetResult();

        if (dataFormat == DataFormats.Files)
            return DataTransfer.TryGetFilesAsync().GetAwaiter().GetResult();

        if (dataFormat == DataFormats.FileNames)
        {
            return DataTransfer
                .TryGetFilesAsync().GetAwaiter().GetResult()
                ?.Select(file => file.TryGetLocalPath())
                .Where(path => path is not null)
                .ToArray();
        }

        return DataTransfer.TryGetValueAsync<object?>(DataFormat.FromSystemName(dataFormat)).GetAwaiter().GetResult();
    }
}
