#nullable enable

using System;
using System.IO;
using System.Linq;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Native.Interop;
using Avalonia.Platform.Storage;

namespace Avalonia.Native;

/// <summary>
/// Wraps a <see cref="IDataTransferItem"/> into a <see cref="IAvnClipboardDataItem"/>.
/// This class is called by native code.
/// </summary>
/// <param name="item">The item to wrap.</param>
internal sealed class DataTransferItemToAvnClipboardDataItemWrapper(IDataTransferItem item)
    : NativeOwned, IAvnClipboardDataItem
{
    private readonly IDataTransferItem _item = item;

    IAvnStringArray IAvnClipboardDataItem.ProvideFormats()
        => new AvnStringArray(_item.Formats.Select(ClipboardDataFormatHelper.ToNativeFormat));

    IAvnClipboardDataValue? IAvnClipboardDataItem.GetValue(string format)
    {
        if (FindDataFormat(format) is { } dataFormat)
        {
            if (DataFormat.Text.Equals(dataFormat))
                return new StringValue(_item.TryGetValue(DataFormat.Text) ?? string.Empty);

            if (DataFormat.File.Equals(dataFormat))
                return _item.TryGetValue(DataFormat.File) is { } file ? new StringValue(file.Path.AbsoluteUri) : null;

            if (DataFormat.Bitmap.Equals(dataFormat))
            {
                if (_item.TryGetValue(DataFormat.Bitmap) is { } bitmap)
                {
                    var memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return new BytesValue(memoryStream.ToArray());
                }

                return null;
            }

            if (dataFormat is DataFormat<string> stringFormat)
                return _item.TryGetValue(stringFormat) is { } stringValue ? new StringValue(stringValue) : null;

            if (dataFormat is DataFormat<byte[]> bytesFormat)
                return _item.TryGetValue(bytesFormat) is { } bytes ? new BytesValue(bytes) : null;
        }

        Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)
            ?.Log(this, "Unsupported data format {Format}", format);

        return null;
    }

    private DataFormat? FindDataFormat(string nativeFormat)
    {
        var formats = _item.Formats;
        var count = formats.Count;
        for (var i = 0; i < count; i++)
        {
            var format = formats[i];
            if (ClipboardDataFormatHelper.ToNativeFormat(format) == nativeFormat)
                return format;
        }

        return null;
    }

    private sealed class StringValue(string value) : NativeOwned, IAvnClipboardDataValue
    {
        private readonly string _value = value;

        int IAvnClipboardDataValue.IsString()
            => true.AsComBool();

        IAvnString IAvnClipboardDataValue.AsString()
            => new AvnString(_value);

        int IAvnClipboardDataValue.ByteLength
            => throw new InvalidOperationException();

        unsafe void IAvnClipboardDataValue.CopyBytesTo(void* buffer)
            => throw new InvalidOperationException();
    }

    private sealed class BytesValue(ReadOnlyMemory<byte> value) : NativeOwned, IAvnClipboardDataValue
    {
        private readonly ReadOnlyMemory<byte> _value = value;

        int IAvnClipboardDataValue.IsString()
            => false.AsComBool();

        IAvnString IAvnClipboardDataValue.AsString()
            => throw new InvalidOperationException();

        int IAvnClipboardDataValue.ByteLength
            => _value.Length;

        unsafe void IAvnClipboardDataValue.CopyBytesTo(void* buffer)
            => _value.Span.CopyTo(new Span<byte>(buffer, _value.Length));
    }
}
