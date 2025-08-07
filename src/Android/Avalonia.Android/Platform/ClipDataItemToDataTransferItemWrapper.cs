using System;
using Android.App;
using Android.Content;
using Avalonia.Android.Platform.Storage;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform;

/// <summary>
/// Wraps a <see cref="ClipData.Item"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="item">The clip data item.</param>
/// <param name="owner">The data transfer owning this item.</param>
internal sealed class ClipDataItemToDataTransferItemWrapper(ClipData.Item item, ClipDataToDataTransferWrapper owner)
    : PlatformDataTransferItem
{
    private readonly ClipData.Item _item = item;
    private readonly ClipDataToDataTransferWrapper _owner = owner;

    protected override DataFormat[] ProvideFormats()
        => _owner.Formats; // There's no "format per item", assume each item handle all formats

    protected override object? TryGetCore(DataFormat format)
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
