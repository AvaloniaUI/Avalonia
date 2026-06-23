using System.Linq;
using Android.Content;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform;

/// <summary>
/// Wraps a <see cref="ClipData"/> into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="clipData">The clip data.</param>
/// <param name="context">The application context.</param>
internal sealed class ClipDataToDataTransferWrapper(ClipData clipData, Context? context)
    : PlatformDataTransfer
{
    private readonly ClipData _clipData = clipData;

    public Context? Context { get; } = context;

    protected override DataFormat[] ProvideFormats()
    {
        if (_clipData.Description is not { MimeTypeCount: > 0 and var count } clipDescription)
            return [];

        var formats = new DataFormat[count];

        bool hasImage = false;
        for (var i = 0; i < count; ++i)
        {
            formats[i] = AndroidDataFormatHelper.MimeTypeToDataFormat(clipDescription.GetMimeType(i)!);

            if (!hasImage)
                hasImage = formats[i].Identifier.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase);
        }

        if (hasImage)
        {
            formats = [.. formats, DataFormat.Bitmap];
        }

        return formats;
    }

    protected override PlatformDataTransferItem[] ProvideItems()
    {
        var count = _clipData.ItemCount;
        var items = new PlatformDataTransferItem[count];

        for (var i = 0; i < count; ++i)
            items[i] = new ClipDataItemToDataTransferItemWrapper(_clipData.GetItemAt(i)!, this);

        return items;
    }

    public override void Dispose()
    {
    }
}
