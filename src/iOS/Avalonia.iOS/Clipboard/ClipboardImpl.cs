using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Foundation;
using UIKit;
using static Avalonia.iOS.Clipboard.ClipboardDataFormatHelper;

namespace Avalonia.iOS.Clipboard;

internal sealed class ClipboardImpl(UIPasteboard pasteboard)
    : IOwnedClipboardImpl
{
    private readonly UIPasteboard _pasteboard = pasteboard;
    private long _lastChangeCount = long.MinValue;

    public Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        try
        {
            return Task.FromResult(TryGetData());
        }
        catch (Exception ex)
        {
            return Task.FromException<IAsyncDataTransfer?>(ex);
        }
    }

    private IAsyncDataTransfer? TryGetData()
    {
        var dataTransfer = new PasteboardToDataTransferWrapper(_pasteboard, _pasteboard.ChangeCount);

        if (dataTransfer.Formats.Length == 0)
        {
            dataTransfer.Dispose();
            return null;
        }

        return dataTransfer;
    }

    public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        List<NSDictionary>? pasteboardItems = null;

        foreach (var dataTransferItem in dataTransfer.Items)
        {
            if (await TryCreatePasteboardItemAsync(dataTransferItem) is not { } pasteboardItem)
                continue;

            pasteboardItems ??= new();
            pasteboardItems.Add(pasteboardItem);
        }

        _pasteboard.Items = pasteboardItems?.ToArray() ?? [];
        _lastChangeCount = _pasteboard.ChangeCount;
    }

    private async Task<NSDictionary?> TryCreatePasteboardItemAsync(IAsyncDataTransferItem dataTransferItem)
    {
        NSMutableDictionary? pasteboardItem = null;

        foreach (var dataFormat in dataTransferItem.Formats)
        {
            var data = await TryGetFoundationDataAsync(dataTransferItem, dataFormat);
            if (data is null)
                continue;

            var type = ToSystemType(dataFormat);
            pasteboardItem ??= new();
            pasteboardItem[type] = data;
        }

        return pasteboardItem;
    }

    private async Task<NSObject?> TryGetFoundationDataAsync(IAsyncDataTransferItem dataTransferItem, DataFormat format)
    {
        if (format.Equals(DataFormat.Text))
        {
            var text = await dataTransferItem.TryGetValueAsync(DataFormat.Text) ?? string.Empty;
            return (NSString)text;
        }

        if (format.Equals(DataFormat.File))
        {
            var file = await dataTransferItem.TryGetValueAsync(DataFormat.File);
            return file is null ? null : (NSString)file.Path.AbsoluteUri;
        }

        if (format.Equals(DataFormat.Bitmap))
        {
            var bitmap = await dataTransferItem.TryGetValueAsync(DataFormat.Bitmap);
            if (bitmap is null)
                return null;
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, 100);
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var data = NSData.FromStream(memoryStream)!;
            return UIImage.LoadFromData(data);
        }

        if (format is DataFormat<string> stringFormat)
        {
            var stringValue = await dataTransferItem.TryGetValueAsync(stringFormat);
            return stringValue is null ? null : (NSString)stringValue;
        }

        if (format is DataFormat<byte[]> bytesFormat)
        {
            var bytes = await dataTransferItem.TryGetValueAsync(bytesFormat);
            return bytes is null ? null : NSData.FromArray(bytes);
        }

        Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)
            ?.Log(this, "Unsupported data format {Format}", format);

        return null;
    }

    public Task ClearAsync()
    {
        try
        {
            _pasteboard.Items = [];
            _lastChangeCount = _pasteboard.ChangeCount;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public Task<bool> IsCurrentOwnerAsync()
        => Task.FromResult(_lastChangeCount == _pasteboard.ChangeCount);
}
