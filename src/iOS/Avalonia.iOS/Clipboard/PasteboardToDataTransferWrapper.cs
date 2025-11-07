using Avalonia.Input;
using Avalonia.Input.Platform;
using UIKit;

namespace Avalonia.iOS.Clipboard;

internal sealed class PasteboardToDataTransferWrapper(UIPasteboard pasteboard, long changeCount)
    : PlatformDataTransfer
{
    private readonly UIPasteboard _pasteboard = pasteboard;
    private readonly long _changeCount = changeCount;

    protected override DataFormat[] ProvideFormats()
    {
        if (_changeCount != _pasteboard.ChangeCount)
            return [];

        var types = _pasteboard.Types;
        var formats = new DataFormat[types.Length];
        for (var i = 0; i < formats.Length; ++i)
            formats[i] = ClipboardDataFormatHelper.ToDataFormat(types[i]);
        return formats;
    }

    protected override PlatformDataTransferItem[] ProvideItems()
    {
        if (_changeCount != _pasteboard.ChangeCount)
            return [];

        var pasteboardItems = _pasteboard.Items;
        var items = new PlatformDataTransferItem[pasteboardItems.Length];
        for (var i = 0; i < pasteboardItems.Length; ++i)
            items[i] = new PasteboardItemToDataTransferItemWrapper(pasteboardItems[i]);
        return items;
    }

    public override void Dispose()
    {
    }
}
