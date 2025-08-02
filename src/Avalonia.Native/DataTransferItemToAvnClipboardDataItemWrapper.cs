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
        var dataFormat = ClipboardDataFormatHelper.ToDataFormat(format);
        var data = _item.TryGetAsync(dataFormat).GetAwaiter().GetResult();

        if (DataFormat.Text.Equals(dataFormat))
            return new StringValue(Convert.ToString(data) ?? string.Empty);

        if (DataFormat.File.Equals(dataFormat))
        {
            var file = GetTypedData<IStorageItem>(data, dataFormat);
            return file is null ? null : new StringValue(file.Path.AbsoluteUri);
        }

        switch (data)
        {
            case null:
                return null;

            case byte[] bytes:
                return new BytesValue(bytes);

            case Memory<byte> bytes:
                return new BytesValue(bytes);

            case string str:
                return new StringValue(str);

            case Stream stream:
            {
                var length = (int)(stream.Length - stream.Position);
                var buffer = new byte[length];
                stream.ReadExactly(buffer, 0, length);
                return new BytesValue(buffer.AsMemory(length));
            }

            default:
                Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)?.Log(
                this,
                "Unsupported value type {Type} for data format {Format}",
                data.GetType(),
                dataFormat);
                return null;
        }

        static T? GetTypedData<T>(object? data, DataFormat format) where T : class
            => data switch
            {
                null => null,
                T value => value,
                _ => throw new InvalidOperationException(
                    $"Expected a value of type {typeof(T)} for data format {format}, got {data.GetType()} instead.")
            };
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
