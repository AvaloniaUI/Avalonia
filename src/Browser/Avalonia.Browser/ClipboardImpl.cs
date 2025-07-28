using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using static Avalonia.Browser.BrowserDataFormatHelper;
using static Avalonia.Browser.Interop.InputHelper;

namespace Avalonia.Browser
{
    internal sealed class ClipboardImpl : IClipboardImpl
    {
        public async Task<IDataTransfer?> TryGetDataAsync()
        {
            var jsItems = await ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis).ConfigureAwait(false);
            return jsItems.GetPropertyAsInt32("length") == 0 ? null : new BrowserDataTransfer(jsItems);
        }

        public async Task SetDataAsync(IDataTransfer dataTransfer)
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

        private async Task TryAddItemAsync(IDataTransferItem dataTransferItem, JSObject source)
        {
            JSObject? writeableItem = null;

            try
            {
                foreach (var dataFormat in dataTransferItem.Formats)
                {
                    var formatString = ToBrowserFormat(dataFormat);
                    if (!IsClipboardFormatSupported(formatString))
                        continue;

                    var data = await dataTransferItem.TryGetAsync(dataFormat);

                    if (DataFormat.Text.Equals(dataFormat))
                    {
                        AddStringToItem(Convert.ToString(data) ?? string.Empty);
                        continue;
                    }

                    switch (data)
                    {
                        case null:
                            break;

                        case byte[] bytes:
                            AddBytesToItem(bytes.AsSpan());
                            break;

                        case Memory<byte> bytes:
                            AddBytesToItem(bytes.Span);
                            break;

                        case string str:
                            AddStringToItem(str);
                            break;

                        case Stream stream:
                        {
                            var length = (int)(stream.Length - stream.Position);
                            var buffer = ArrayPool<byte>.Shared.Rent(length);

                            try
                            {
                                await stream.ReadExactlyAsync(buffer, 0, length);
                                AddBytesToItem(buffer.AsSpan(0, length));
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }
                            break;
                        }

                        default:
                            Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)?.Log(
                                this,
                                "Unsupported value type {Type} for data format {Format}",
                                data.GetType(),
                                dataFormat);
                            break;
                    }

                    void AddStringToItem(string str)
                    {
                        writeableItem ??= CreateWriteableClipboardItem(source);
                        AddStringToWriteableClipboardItem(writeableItem, formatString, str);
                    }

                    void AddBytesToItem(Span<byte> bytes)
                    {
                        writeableItem ??= CreateWriteableClipboardItem(source);
                        AddBytesToWriteableClipboardItem(writeableItem, formatString, bytes);
                    }
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
}
