using System;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.iOS.Storage;
using Avalonia.Media.Imaging;
using Foundation;
using UIKit;

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
        // Handle images specially without ToSystemType, as we may have multiple UTI types for images.
        if (DataFormat.Bitmap.Equals(format))
        {
            NSObject? imageValue;

            if (_item.TryGetValue((NSString)ClipboardDataFormatHelper.UTTypePng, out imageValue)
                || _item.TryGetValue((NSString)ClipboardDataFormatHelper.UTTypeJpeg, out imageValue))
            {
                // keep imageValue as is, it can be either UIImage or NSData, in either case Bitmap can handle it directly.
            }
            else if (_item.TryGetValue((NSString)ClipboardDataFormatHelper.UTTypeImage, out imageValue)
                     || _item.TryGetValue((NSString)ClipboardDataFormatHelper.UTTypeTiff, out imageValue))
            {
                // if it's NSData, we need to convert it to UIImage first, as TIFF is not directly supported by Bitmap.
                if (imageValue is NSData imageData)
                {
                    imageValue = UIImage.LoadFromData(imageData);
                    imageData.Dispose();
                }
            }

            switch (imageValue)
            {
                case UIImage image:
                {
                    using var pngData = image.AsPNG()!;
                    using var pngStream = pngData.AsStream();
                    return new Bitmap(pngStream);
                }
                case NSData data:
                {
                    using var dataStream = data.AsStream();
                    return new Bitmap(dataStream);
                }
                default:
                    return null;
            }
        }

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

    private static unsafe string? TryConvertToString(NSObject? value)
        => value switch
        {
            NSString str => str,
            NSData data => Encoding.Unicode.GetString(new ReadOnlySpan<byte>((void*)data.Bytes, (int)data.Length)),
            _ => null
        };

    private static byte[]? TryConvertToBytes(NSObject? value)
        => value switch
        {
            NSData data => data.ToArray(),
            NSString str => Encoding.Unicode.GetBytes((string)str),
            _ => null
        };
}
