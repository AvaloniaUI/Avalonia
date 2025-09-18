using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Avalonia.Compatibility;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

/// <summary>
/// Wraps a <see cref="IDataTransfer"/> into a legacy <see cref="IDataObject"/>.
/// </summary>
[Obsolete]
internal sealed class DataTransferToDataObjectWrapper(IDataTransfer dataTransfer) : IDataObject
{
    // Compatibility with WinForms + WPF...
    // TODO12: remove
    internal static ReadOnlySpan<byte> SerializedObjectGuid
        => [
            // FD9EA796-3B13-4370-A679-56106BB288FB
            0x96, 0xa7, 0x9e, 0xfd,
            0x13, 0x3b,
            0x70, 0x43,
            0xa6, 0x79, 0x56, 0x10, 0x6b, 0xb2, 0x88, 0xfb
        ];

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

        var typedFormat = DataFormat.CreateBytesPlatformFormat(dataFormat);
        var bytes = DataTransfer.TryGetValue(typedFormat);

        // Our Win32 backend used to automatically serialize/deserialize objects using the BinaryFormatter.
        // Only keep that behavior for compatibility with IDataObject.
        // TODO12: Completely remove in v12.
        if (OperatingSystemEx.IsWindows() && bytes is not null && bytes.AsSpan().StartsWith(SerializedObjectGuid))
        {
            using var stream = new MemoryStream(bytes);
            stream.Position = SerializedObjectGuid.Length;
            return DeserializeUsingBinaryFormatter(stream);
        }

        return bytes;
    }

    // TODO12: remove
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    private static object DeserializeUsingBinaryFormatter(MemoryStream stream)
    {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        return new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
    }
}
