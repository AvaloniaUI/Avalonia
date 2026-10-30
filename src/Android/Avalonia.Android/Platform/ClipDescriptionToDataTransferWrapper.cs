using Android.Content;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform;

/// <summary>
/// Wrap a <see cref="ClipDescription"/>. Android doesn't allow reading clip data on events other than the Drop DragEvent.
/// So we provide a wrapper with no data access for those events
/// </summary>
/// <param name="description"></param>
internal sealed class ClipDescriptionToDataTransferWrapper(ClipDescription description)
    : PlatformDataTransfer
{
    private readonly ClipDescription _description = description;

    protected override DataFormat[] ProvideFormats()
    {
        if (_description is not { MimeTypeCount: > 0 and var count } clipDescription)
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
        return [];
    }

    public override void Dispose()
    {
    }
}
