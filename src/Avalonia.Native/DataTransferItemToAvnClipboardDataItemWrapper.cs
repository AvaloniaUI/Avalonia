using System;
using System.IO;
using System.Linq;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Native.Interop;

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
                    return new StreamValue(memoryStream);
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

        IntPtr IAvnClipboardDataValue.ByteLength
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

        IntPtr IAvnClipboardDataValue.ByteLength
            => new(_value.Length);

        unsafe void IAvnClipboardDataValue.CopyBytesTo(void* buffer)
            => _value.Span.CopyTo(new Span<byte>(buffer, _value.Length));
    }
    
    private sealed class StreamValue(MemoryStream value) : NativeOwned, IAvnClipboardDataValue
    {
        private readonly MemoryStream _value = value;
        private readonly byte[] _buffer = new byte[1024 * 1024];

        int IAvnClipboardDataValue.IsString()
            => false.AsComBool();

        IAvnString IAvnClipboardDataValue.AsString()
            => throw new InvalidOperationException();

        IntPtr IAvnClipboardDataValue.ByteLength
#pragma warning disable CA2020 // overflow in unchecked context
            => (IntPtr)_value.Length;
#pragma warning restore CA2020

        unsafe void IAvnClipboardDataValue.CopyBytesTo(void* output)
        {
            long totalCopied = 0;

            while (true)
            {
                var read = _value.Read(_buffer, 0, _buffer.Length);
                if (read == 0)
                    break;

                var destinationSpan = new Span<byte>((byte*)output + totalCopied, read);
                _buffer.AsSpan(0, read).CopyTo(destinationSpan);

                totalCopied += read;
            }
        }
    }
}
