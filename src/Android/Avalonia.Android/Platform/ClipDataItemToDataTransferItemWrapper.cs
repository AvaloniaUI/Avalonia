using System.IO;
using Android.App;
using Android.Content;
using Avalonia.Android.Platform.Storage;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

namespace Avalonia.Android.Platform;

/// <summary>
/// Wraps a <see cref="ClipData.Item"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="item">The clip data item.</param>
/// <param name="owner">The data transfer owning this item.</param>
internal sealed class ClipDataItemToDataTransferItemWrapper(ClipData.Item item, ClipDataToDataTransferWrapper owner)
    : PlatformDataTransferItem
{
    private readonly ClipDataToDataTransferWrapper _owner = owner;

    protected override DataFormat[] ProvideFormats()
        => _owner.Formats; // There's no "format per item", assume each item handle all formats

    protected override object? TryGetRawCore(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return item.CoerceToText(_owner.Context);

        if (format is DataFormat<string>)
            return TryGetAsString();

        if (DataFormat.File.Equals(format))
        {
            return item.Uri is { Scheme: "file" or "content" } fileUri && _owner.Context is Activity activity ?
                    AndroidStorageFile.CreateItem(activity, fileUri) :
                    null;
        }
        else if (DataFormat.Bitmap.Equals(format))
        {
            var file = item.Uri is { Scheme: "file" or "content" } fileUri && _owner.Context is Activity activity ?
                    AndroidStorageFile.CreateItem(activity, fileUri) :
                    null;

            if (file is AndroidStorageFile storageFile)
            {
                using var stream = storageFile.OpenRead();

                if (stream != null)
                {
                    return new Bitmap(stream);
                }
            }
        }
        else if (format is DataFormat<byte[]>)
        {
            var file = item.Uri is { Scheme: "file" or "content" } fileUri && _owner.Context is Activity activity ?
                    AndroidStorageFile.CreateItem(activity, fileUri) :
                    null;

            if (file is AndroidStorageFile storageFile)
            {
                using var stream = storageFile.OpenRead();

                using var mem = new MemoryStream();
                stream.CopyTo(mem);
                return mem.ToArray();
            }
        }

        return null;
    }

    private string? TryGetAsString()
    {
        if (item.Text is { } text)
            return text;

        if (item.HtmlText is { } htmlText)
            return htmlText;

        if (item.Uri is { } uri)
            return uri.ToString();

        if (item.Intent is { } intent)
            return intent.ToUri(IntentUriType.Scheme);

        return null;
    }
}
