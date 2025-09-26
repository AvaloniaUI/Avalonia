using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Avalonia.Compatibility;
using Avalonia.Logging;

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

    protected override object? TryGetRawCore(DataFormat format)
    {
        var index = Array.IndexOf(Formats, format);
        if (index < 0)
            return null;

        // We should never have DataFormat.File here, it's been handled by DataObjectToDataTransferWrapper.
        Debug.Assert(!DataFormat.File.Equals(format));

        var formatString = _formatStrings[index];
        var data = _dataObject.Get(formatString);

        if (DataFormat.Text.Equals(format))
            return Convert.ToString(data) ?? string.Empty;

        if (format is DataFormat<string>)
            return Convert.ToString(data);

        if (format is DataFormat<byte[]>)
            return ConvertLegacyDataToBytes(format, data);

        return null;
    }

    private static byte[]? ConvertLegacyDataToBytes(DataFormat format, object? data)
    {
        switch (data)
        {
            case null:
                return null;

            case byte[] bytes:
                return bytes;

            case string str:
                return OperatingSystemEx.IsWindows() || OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsIOS() ?
                    Encoding.Unicode.GetBytes(str) :
                    Encoding.UTF8.GetBytes(str);

            case Stream stream:
                var length = (int)(stream.Length - stream.Position);
                var buffer = new byte[length];

                stream.ReadExactly(buffer, 0, length);
                return buffer;

            default:
                if (OperatingSystemEx.IsWindows())
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(
                        null,
                        "Using BinaryFormatter to serialize data format {Format}. This won't be supported in Avalonia v12. Prefer passing a byte[] or Stream instead.",
                        format);

                    return SerializeUsingBinaryFormatter(data);
                }

                return null;
        }
    }

    // TODO12: remove
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    private static byte[] SerializeUsingBinaryFormatter(object data)
    {
        var stream = new MemoryStream();
        var serializedGuid = DataTransferToDataObjectWrapper.SerializedObjectGuid;

#if NET6_0_OR_GREATER
        stream.Write(serializedGuid);
#else
        stream.Write(serializedGuid.ToArray(), 0, serializedGuid.Length);
#endif

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        new BinaryFormatter().Serialize(stream, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

        return stream.GetBuffer();
    }
}
