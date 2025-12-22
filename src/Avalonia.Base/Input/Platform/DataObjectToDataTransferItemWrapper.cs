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
                return BinaryFormatterHelper.TrySerializeUsingBinaryFormatter(data, format);
        }
    }
}
