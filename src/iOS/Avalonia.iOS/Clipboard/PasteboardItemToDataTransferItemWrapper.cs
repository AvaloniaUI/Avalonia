using System;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.iOS.Storage;
using Foundation;

namespace Avalonia.iOS.Clipboard;

internal sealed class PasteboardItemToDataTransferItemWrapper(NSDictionary item)
    : PlatformDataTransferItem
{
    private readonly NSDictionary _item = item; // key: NSString* type, value: id value

    protected override DataFormat[] ProvideFormats()
    {
        var types = _item.Keys;
        var formats = new DataFormat[types.Length];
        for (var i = 0; i < types.Length; ++i)
            formats[i] = ClipboardDataFormatHelper.ToDataFormat((NSString)types[i]);
        return formats;
    }

    protected override object? TryGetRawCore(DataFormat format)
    {
        var type = ClipboardDataFormatHelper.ToSystemType(format);
        if (!_item.TryGetValue((NSString)type, out var value))
            return null;

        if (DataFormat.Text.Equals(format))
            return (value as NSString)?.ToString();

        if (DataFormat.File.Equals(format))
            return (value as NSUrl)?.FilePathUrl is { } filePathUrl ? IOSStorageItem.CreateItem(filePathUrl) : null;

        if (format is DataFormat<string>)
            return TryConvertToString(value);

        if (format is DataFormat<byte[]>)
            return TryConvertToBytes(value);

        return null;
    }

    private static unsafe string? TryConvertToString(NSObject value)
        => value switch
        {
            NSString str => str,
            NSData data => Encoding.Unicode.GetString(new ReadOnlySpan<byte>((void*)data.Bytes, (int)data.Length)),
            _ => null
        };

    private static byte[]? TryConvertToBytes(NSObject value)
        => value switch
        {
            NSData data => data.ToArray(),
            NSString str => Encoding.Unicode.GetBytes((string)str),
            _ => null
        };
}
