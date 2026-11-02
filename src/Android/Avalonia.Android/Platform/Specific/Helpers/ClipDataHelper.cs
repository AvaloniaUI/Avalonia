using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Avalonia.Input;
using Avalonia.Logging;
using AndroidUri = Android.Net.Uri;

namespace Avalonia.Android.Platform.Specific.Helpers
{
    internal static class ClipDataHelper
    {
        public static async Task<ClipData?> GetClipData(IAsyncDataTransfer dataTransfer, object logSource)
        {
            ClipData? clipData = null;
            var mimeTypes = dataTransfer.Formats
                .Where(f => f.Kind != DataFormatKind.InProcess)
                .Select(AndroidDataFormatHelper.DataFormatToMimeType)
                .ToArray();

            ClipData.Item? firstItem = null;
            List<ClipData.Item>? additionalItems = null;

            foreach (var dataTransferItem in dataTransfer.Items)
            {
                if (await TryCreateDataItemAsync(dataTransferItem, logSource) is not { } clipDataItem)
                {
                    continue;
                }

                if (firstItem is null)
                    firstItem = clipDataItem;
                else
                    (additionalItems ??= new()).Add(clipDataItem);
            }

            if (firstItem is null)
            {
                return clipData;
            }

            clipData = new ClipData((string?)null, mimeTypes, firstItem);
            if (additionalItems is not null)
            {
                foreach (var additionalItem in additionalItems)
                    clipData.AddItem(additionalItem);
            }

            return clipData;
        }

        public static async Task<ClipData?> GetClipData(IDataTransfer dataTransfer, object logSource)
        {
            ClipData? clipData = null;
            var mimeTypes = dataTransfer.Formats
                .Where(f => f.Kind != DataFormatKind.InProcess)
                .Select(AndroidDataFormatHelper.DataFormatToMimeType)
                .ToArray();

            ClipData.Item? firstItem = null;
            List<ClipData.Item>? additionalItems = null;

            foreach (var dataTransferItem in dataTransfer.Items)
            {
                if (await TryCreateDataItemAsync(dataTransferItem, logSource) is not { } clipDataItem)
                {
                    continue;
                }

                if (firstItem is null)
                    firstItem = clipDataItem;
                else
                    (additionalItems ??= new()).Add(clipDataItem);
            }

            if (firstItem is null)
            {
                return clipData;
            }

            clipData = new ClipData((string?)null, mimeTypes, firstItem);
            if (additionalItems is not null)
            {
                foreach (var additionalItem in additionalItems)
                    clipData.AddItem(additionalItem);
            }

            return clipData;
        }

        public static async Task<ClipData.Item?> TryCreateDataItemAsync(IAsyncDataTransferItem item, object logSource)
        {
            var hasFormats = false;

            // Create the item from the first format returning a supported value.
            foreach (var dataFormat in item.Formats)
            {
                if (dataFormat.Kind == DataFormatKind.InProcess)
                    continue;

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
                    logSource,
                    "No compatible value found for data transfer item with formats {Formats}",
                    string.Join(", ", item.Formats));
            }

            return null;
        }

        public static async Task<ClipData.Item?> TryCreateDataItemAsync(IDataTransferItem item, object logSource)
        {
            var hasFormats = false;

            // Create the item from the first format returning a supported value.
            foreach (var dataFormat in item.Formats)
            {
                if (dataFormat.Kind == DataFormatKind.InProcess)
                    continue;

                hasFormats = true;

                if (DataFormat.Text.Equals(dataFormat))
                {
                    var text = item.TryGetValue(DataFormat.Text);
                    return new ClipData.Item(text);
                }

                if (DataFormat.File.Equals(dataFormat))
                {
                    var storageItem = item.TryGetValue(DataFormat.File);
                    if (storageItem is null)
                        continue;

                    return new ClipData.Item(AndroidUri.Parse(storageItem.Path.OriginalString));
                }

                if (dataFormat is DataFormat<string> stringFormat)
                {
                    var stringValue = item.TryGetValue(stringFormat);
                    if (stringValue is null)
                        continue;

                    return new ClipData.Item(stringValue);
                }
            }

            if (hasFormats)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.AndroidPlatform)?.Log(
                    logSource,
                    "No compatible value found for data transfer item with formats {Formats}",
                    string.Join(", ", item.Formats));
            }

            return null;
        }
    }
}
