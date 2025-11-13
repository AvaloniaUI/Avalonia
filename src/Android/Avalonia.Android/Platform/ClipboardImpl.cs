using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using AndroidUri = Android.Net.Uri;

namespace Avalonia.Android.Platform
{
    internal sealed class ClipboardImpl(ClipboardManager? clipboardManager, Context? context)
        : IClipboardImpl
    {
        private readonly ClipboardManager? _clipboardManager = clipboardManager;
        private readonly Context? _context = context;

        public Task<IAsyncDataTransfer?> TryGetDataAsync()
        {
            try
            {
                return Task.FromResult<IAsyncDataTransfer?>(TryGetData());
            }
            catch (Exception ex)
            {
                return Task.FromException<IAsyncDataTransfer?>(ex);
            }
        }

        private ClipDataToDataTransferWrapper? TryGetData()
            => _clipboardManager?.PrimaryClip is { } clipData ?
                new ClipDataToDataTransferWrapper(clipData, _context) :
                null;

        public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
        {
            if (_clipboardManager is null)
                return;

            var mimeTypes = dataTransfer.Formats
                .Select(AndroidDataFormatHelper.DataFormatToMimeType)
                .ToArray();

            ClipData.Item? firstItem = null;
            List<ClipData.Item>? additionalItems = null;

            foreach (var dataTransferItem in dataTransfer.Items)
            {
                if (await TryCreateDataItemAsync(dataTransferItem) is not { } clipDataItem)
                    continue;

                if (firstItem is null)
                    firstItem = clipDataItem;
                else
                    (additionalItems ??= new()).Add(clipDataItem);
            }

            if (firstItem is null)
            {
                Clear();
                return;
            }

            var clipData = new ClipData((string?)null, mimeTypes, firstItem);

            if (additionalItems is not null)
            {
                foreach (var additionalItem in additionalItems)
                    clipData.AddItem(additionalItem);
            }

            _clipboardManager.PrimaryClip = clipData;
        }

        private async Task<ClipData.Item?> TryCreateDataItemAsync(IAsyncDataTransferItem item)
        {
            var hasFormats = false;

            // Create the item from the first format returning a supported value.
            foreach (var dataFormat in item.Formats)
            {
                hasFormats = true;

                if (DataFormat.Text.Equals(dataFormat))
                {
                    var text = await item.TryGetValueAsync(DataFormat.Text);
                    return new ClipData.Item(text);
                }

                if (DataFormat.File.Equals(dataFormat))
                {
                    var storageItem = await item.TryGetValueAsync(DataFormat.File);
                    if (storageItem is null)
                        continue;

                    return new ClipData.Item(AndroidUri.Parse(storageItem.Path.OriginalString));
                }

                if (dataFormat is DataFormat<string> stringFormat)
                {
                    var stringValue = await item.TryGetValueAsync(stringFormat);
                    if (stringValue is null)
                        continue;

                    return new ClipData.Item(stringValue);
                }
            }

            if (hasFormats)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.AndroidPlatform)?.Log(
                    this,
                    "No compatible value found for data transfer item with formats {Formats}",
                    string.Join(", ", item.Formats));
            }

            return null;
        }

        public Task ClearAsync()
        {
            try
            {
                Clear();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        private void Clear()
        {
            if (_clipboardManager is null)
                return;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                _clipboardManager.ClearPrimaryClip();
            else
                _clipboardManager.PrimaryClip = ClipData.NewPlainText(null, string.Empty);
        }
    }
}
