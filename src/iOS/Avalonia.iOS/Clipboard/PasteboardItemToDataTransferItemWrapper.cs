using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.iOS.Storage;
using Foundation;

namespace Avalonia.iOS.Clipboard;

internal sealed class PasteboardItemToDataTransferItemWrapper(NSDictionary item)
    : PlatformSyncDataTransferItem
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

    protected override object? TryGetCore(DataFormat format)
    {
        var type = ClipboardDataFormatHelper.ToSystemType(format);
        if (!_item.TryGetValue((NSString)type, out var value))
            return null;

        if (DataFormat.Text.Equals(format))
            return (value as NSString)?.ToString();

        if (DataFormat.File.Equals(format))
            return (value as NSUrl)?.FilePathUrl is { } filePathUrl ? IOSStorageItem.CreateItem(filePathUrl) : null;

        return value switch
        {
            NSString str => (string)str,
            NSData data => data.ToArray(),
            _ => null
        };
    }
}
