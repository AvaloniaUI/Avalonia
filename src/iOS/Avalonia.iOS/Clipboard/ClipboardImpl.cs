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

namespace Avalonia.iOS.Clipboard
{
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
                if (await dataTransferItem.TryGetAsync(dataFormat) is not { } data)
                    continue;

                var type = ToSystemType(dataFormat);
                var foundationData = await DataToFoundationDataAsync(data, dataFormat);
                pasteboardItem ??= new();
                pasteboardItem[type] = foundationData;
            }

            return pasteboardItem;
        }

        private async Task<NSObject?> DataToFoundationDataAsync(object? data, DataFormat dataFormat)
        {
            switch (data)
            {
                case null:
                    return null;

                case byte[] bytes:
                    return NSData.FromArray(bytes);

                case Memory<byte> bytes:
                    return NSData.FromArray(bytes.ToArray());

                case string str:
                    return (NSString)str;

                case Stream stream:
                {
                    var length = (int)(stream.Length - stream.Position);
                    var buffer = new byte[length];
                    await stream.ReadExactlyAsync(buffer, 0, length).ConfigureAwait(false);
                    return NSData.FromArray(buffer);
                }

                default:
                    Logger.TryGet(LogEventLevel.Warning, LogArea.IOSPlatform)?.Log(
                        this,
                        "Unsupported value type {Type} for data format {Format}",
                        data.GetType(),
                        dataFormat);
                    return null;
            }
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
}
