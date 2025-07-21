using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser.Storage;
using Avalonia.Input.Platform;
using static Avalonia.Browser.BrowserDataFormatHelper;
using static Avalonia.Browser.Interop.InputHelper;

namespace Avalonia.Browser;

/// <summary>
/// Wraps a ReadableClipboardItem (a custom type defined in input.ts) into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="jsItem">The ReadableClipboardItem object.</param>
internal sealed class BrowserDataTransferItem(JSObject jsItem) : IDataTransferItem, IDisposable
{
    private readonly JSObject _jsItem = jsItem; // JS type: ReadableClipboardItem
    private DataFormat[]? _formats;

    public DataFormat[] Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
            {
                var formatStrings = GetReadableClipboardItemFormats(_jsItem);

                var formats = new DataFormat[formatStrings.Length];
                for (var i = 0; i < formatStrings.Length; ++i)
                    formats[i] = ToDataFormat(formatStrings[i]);
                return formats;
            }
        }
    }

    /// <inheritdoc />
    public bool Contains(DataFormat format)
        => Array.IndexOf(Formats, format) >= 0;

    public bool ContainsAny(ReadOnlySpan<DataFormat> formats)
        => Formats.AsSpan().IndexOfAny(formats) >= 0;

    /// <inheritdoc />
    public IEnumerable<DataFormat> GetFormats()
        => Formats;

    /// <inheritdoc />
    public async Task<object?> TryGetAsync(DataFormat format)
    {
        var formatString = ToBrowserFormat(format);
        var value = await TryGetReadableClipboardItemValueAsync(_jsItem, formatString).ConfigureAwait(false);

        return value?.GetPropertyAsString("type") switch
        {
            "string" => value.GetPropertyAsString("value"),
            "bytes" => value.GetPropertyAsByteArray("value"),
            "file" => value.GetPropertyAsJSObject("value") is { } jsFile ? new JSStorageFile(jsFile) : null,
            _ => null
        };
    }

    public void Dispose()
        => _jsItem.Dispose();
}
