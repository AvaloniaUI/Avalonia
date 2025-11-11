using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using static Avalonia.Browser.BrowserDataFormatHelper;
using static Avalonia.Browser.Interop.InputHelper;

namespace Avalonia.Browser;

internal sealed class ClipboardImpl : IClipboardImpl
{
    public async Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        var jsItems = await ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis).ConfigureAwait(false);
        return jsItems.GetPropertyAsInt32("length") == 0 ? null : new BrowserClipboardDataTransfer(jsItems);
    }

    public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        using var source = CreateWriteableClipboardSource();

        foreach (var dataTransferItem in dataTransfer.Items)
        {
            // No ConfigureAwait(false) here: we want TryGetAsync() for next items to be called on the initial thread.
            await TryAddItemAsync(dataTransferItem, source);
        }

        // However, ConfigureAwait(false) is fine here: we're not doing anything after.
        await WriteClipboardAsync(BrowserWindowingPlatform.GlobalThis, source).ConfigureAwait(false);
    }

    private async Task TryAddItemAsync(IAsyncDataTransferItem dataTransferItem, JSObject source)
    {
        JSObject? writeableItem = null;

        try
        {
            foreach (var format in dataTransferItem.Formats)
            {
                var formatString = ToBrowserFormat(format);
                if (!IsClipboardFormatSupported(formatString))
                    continue;

                if (DataFormat.Text.Equals(format))
                {
                    var text = await dataTransferItem.TryGetValueAsync(DataFormat.Text) ?? string.Empty;
                    writeableItem ??= CreateWriteableClipboardItem(source);
                    AddStringToWriteableClipboardItem(writeableItem, formatString, text);
                    continue;
                }

                if(DataFormat.Bitmap.Equals(format))
                {
                    var bitmap = await dataTransferItem.TryGetValueAsync(DataFormat.Bitmap);
                    if (bitmap != null)
                    {
                        using var stream = new MemoryStream();
                        bitmap.Save(stream);

                        writeableItem ??= CreateWriteableClipboardItem(source);
                        AddBytesToWriteableClipboardItem(writeableItem, formatString, stream.ToArray());
                    }

                    continue;
                }

                if (format is DataFormat<string> stringFormat)
                {
                    var stringValue = await dataTransferItem.TryGetValueAsync(stringFormat);
                    if (stringValue is not null)
                    {
                        writeableItem ??= CreateWriteableClipboardItem(source);
                        AddStringToWriteableClipboardItem(writeableItem, formatString, stringValue);
                    }
                    continue;
                }

                if (format is DataFormat<byte[]> bytesFormat)
                {
                    var bytes = await dataTransferItem.TryGetValueAsync(bytesFormat);
                    if (bytes is not null)
                    {
                        writeableItem ??= CreateWriteableClipboardItem(source);
                        AddBytesToWriteableClipboardItem(writeableItem, formatString, bytes.AsSpan());
                    }
                    continue;
                }

                // Note: DataFormat.File isn't supported, we can't put arbitrary files onto the clipboard
                // on the browser for security reasons.

                Logger.TryGet(LogEventLevel.Warning, LogArea.BrowserPlatform)
                    ?.Log(this, "Unsupported data format {Format}", format);
            }
        }
        finally
        {
            writeableItem?.Dispose();
        }
    }

    public Task ClearAsync()
        => WriteClipboardAsync(BrowserWindowingPlatform.GlobalThis, null);
}
