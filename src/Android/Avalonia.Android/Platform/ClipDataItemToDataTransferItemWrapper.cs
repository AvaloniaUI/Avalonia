using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Avalonia.Android.Platform.Storage;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform;

/// <summary>
/// Wraps a <see cref="ClipData.Item"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="item">The clip data item.</param>
/// <param name="owner">The data transfer owning this item.</param>
internal sealed class ClipDataItemToDataTransferItemWrapper(ClipData.Item item, ClipDataToDataTransferWrapper owner)
    : IDataTransferItem
{
    private readonly ClipData.Item _item = item;
    private readonly ClipDataToDataTransferWrapper _owner = owner;

    public IEnumerable<DataFormat> GetFormats()
        => _owner.Formats; // There's no "format per item", assume each item handle all formats

    public bool Contains(DataFormat format)
        => Array.IndexOf(_owner.Formats, format) >= 0;

    public Task<object?> TryGetAsync(DataFormat format)
    {
        try
        {
            return Task.FromResult(TryGetValue(format));
        }
        catch (Exception ex)
        {
            return Task.FromException<object?>(ex);
        }
    }

    private object? TryGetValue(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return _item.CoerceToText(_owner.Context);

        if (DataFormat.File.Equals(format))
        {
            return _item.Uri is { Scheme: "file" or "content" } fileUri && _owner.Context is Activity activity ?
                AndroidStorageItem.CreateItem(activity, fileUri) :
                null;
        }

        if (_item.Text is { } text)
            return text;

        if (_item.HtmlText is { } htmlText)
            return htmlText;

        if (_item.Intent is { } intent)
            return intent;

        if (_item.Uri is { } androidUri && Uri.TryCreate(androidUri.ToString(), UriKind.Absolute, out var uri))
            return uri;

        return null;
    }
}
