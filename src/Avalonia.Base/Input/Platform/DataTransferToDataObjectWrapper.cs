using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

#pragma warning disable CS0618 // Type or member is obsolete: usages of IDataObject and DataFormats

/// <summary>
/// Wraps a <see cref="IDataTransfer"/> into a legacy <see cref="IDataObject"/>.
/// </summary>
internal sealed class DataTransferToDataObjectWrapper(IDataTransfer dataTransfer) : IDataObject
{
    public IDataTransfer DataTransfer { get; } = dataTransfer;

    public IEnumerable<string> GetDataFormats()
        => DataTransfer.Formats.Select(DataFormats.ToString);

    public bool Contains(string dataFormat)
        => DataTransfer.Contains(DataFormats.ToDataFormat(dataFormat));

    public object? Get(string dataFormat)
    {
        if (dataFormat == DataFormats.Text)
            return DataTransfer.TryGetText();

        if (dataFormat == DataFormats.Files)
            return DataTransfer.TryGetFiles();

        if (dataFormat == DataFormats.FileNames)
        {
            return DataTransfer
                .TryGetFiles()
                ?.Select(file => file.TryGetLocalPath())
                .Where(path => path is not null)
                .ToArray();
        }

        var typedFormat = DataFormat.CreateOperatingSystemFormat(dataFormat);
        return DataTransfer.TryGetValue<object?>(typedFormat);
    }
}
