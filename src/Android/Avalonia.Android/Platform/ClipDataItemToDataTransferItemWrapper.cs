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
    private readonly ClipData.Item _item = item;
    private readonly ClipDataToDataTransferWrapper _owner = owner;

    protected override DataFormat[] ProvideFormats()
        => _owner.Formats; // There's no "format per item", assume each item handle all formats

    protected override object? TryGetRawCore(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return _item.CoerceToText(_owner.Context);

        if (format is DataFormat<string>)
            return TryGetAsString();

        var file = item.Uri is { Scheme: "file" or "content" } fileUri && _owner.Context is Activity activity ?
                AndroidStorageFile.CreateItem(activity, fileUri) :
                null;

        if(file != null)
        {
            if (DataFormat.File.Equals(format))
            {
                return file;
            }

            try
            {
                if (DataFormat.Image.Equals(format))
                {
                    Bitmap? image = null;

                    if (file is AndroidStorageFile storageFile)
                    {
                        using var stream = storageFile.OpenReadAsync().Result;

                        if (stream != null)
                        {
                            image = new Bitmap(stream);
                        }
                    }

                    return image;
                }

                if (format is DataFormat<byte[]>)
                {
                    if (file is AndroidStorageFile storageFile)
                    {
                        using var stream = storageFile.OpenReadAsync().Result;

                        using var mem = new MemoryStream();
                        stream.CopyTo(mem);
                        return mem.ToArray();
                    }
                }
            }
            finally
            {

                file?.Dispose();
            }
        }

        return null;
    }

    private string? TryGetAsString()
    {
        if (_item.Text is { } text)
            return text;

        if (_item.HtmlText is { } htmlText)
            return htmlText;

        if (_item.Uri is { } uri)
            return uri.ToString();

        if (_item.Intent is { } intent)
            return intent.ToUri(IntentUriType.Scheme);

        return null;
    }
}
