#nullable enable

using System;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Native;

/// <summary>
/// Represents a single item inside a <see cref="ClipboardDataTransfer"/>.
/// </summary>
/// <param name="session">The clipboard session. This is NOT owned by the <see cref="ClipboardDataTransferItem"/>.</param>
/// <param name="itemIndex">The item index.</param>
internal sealed class ClipboardDataTransferItem(ClipboardReadSession session, int itemIndex)
    : PlatformDataTransferItem
{
    private readonly ClipboardReadSession _session = session;
    private readonly int _itemIndex = itemIndex;

    protected override DataFormat[] ProvideFormats()
    {
        using var formats = _session.GetItemFormats(_itemIndex);
        return ClipboardDataFormatHelper.ToDataFormats(formats);
    }

    protected override object? TryGetCore(DataFormat format)
    {
        var nativeFormat = ClipboardDataFormatHelper.ToNativeFormat(format);

        if (DataFormat.Text.Equals(format))
            return TryGetString(nativeFormat);

        if (DataFormat.File.Equals(format))
            return TryGetFile(nativeFormat);

        return TryGetBytes(nativeFormat);
    }

    private object? TryGetString(string nativeFormat)
    {
        using var text = _session.GetItemValueAsString(_itemIndex, nativeFormat);
        return text?.String;
    }

    private object? TryGetFile(string nativeFormat)
    {
        if (AvaloniaLocator.Current.GetService<IStorageProviderFactory>() is not StorageProviderApi storageApi)
            return null;

        using var uriString = _session.GetItemValueAsString(_itemIndex, nativeFormat);
        if (TryGetFilePathUri(uriString?.String, storageApi) is not { } uri)
            return null;

        return storageApi.TryGetStorageItem(uri);
    }

    private static Uri? TryGetFilePathUri(string? uriString, StorageProviderApi storageApi)
    {
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri) || !uri.IsFile)
            return null;

        // macOS may return a file reference URI (e.g. file:///.file/id=6571367.2773272/), convert it to a path URI.
        return uri.AbsolutePath.StartsWith("/.file/id=", StringComparison.Ordinal) ?
            storageApi.TryResolveFileReferenceUri(uri) :
            uri;
    }

    private object? TryGetBytes(string nativeFormat)
    {
        using var bytes = _session.GetItemValueAsBytes(_itemIndex, nativeFormat);
        return bytes?.Bytes;
    }
}
