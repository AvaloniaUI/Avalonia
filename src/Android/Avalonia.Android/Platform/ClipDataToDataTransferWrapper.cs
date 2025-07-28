using Android.Content;
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
        => _clipData.Description?.GetDataFormats() ?? [];

    protected override IDataTransferItem[] ProvideItems()
    {
        var count = _clipData.ItemCount;
        var items = new IDataTransferItem[count];
        for (var i = 0; i < count; ++i)
            items[i] = new ClipDataItemToDataTransferItemWrapper(_clipData.GetItemAt(i)!, this);
        return items;
    }

    public override void Dispose()
    {
    }
}
