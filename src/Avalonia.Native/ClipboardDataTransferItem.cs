using System;
using System.IO;
using System.Text;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

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
        return ClipboardDataFormatHelper.ToDataFormats(formats, _session.IsTextFormat);
    }

    protected override object? TryGetRawCore(DataFormat format)
    {
        var nativeFormat = ClipboardDataFormatHelper.ToNativeFormat(format);

        if (DataFormat.Text.Equals(format))
            return TryGetString(nativeFormat);

        if (DataFormat.File.Equals(format))
            return TryGetFile(nativeFormat);

        if (DataFormat.Bitmap.Equals(format))
            return TryGetBitmap(nativeFormat);

        if (format is DataFormat<string>)
        {
            if (TryGetString(nativeFormat) is { } stringValue)
                return stringValue;
            if (TryGetBytes(nativeFormat) is { } bytes)
                return Encoding.Unicode.GetString(bytes);
            return null;
        }

        if (format is DataFormat<byte[]>)
        {
            if (TryGetBytes(nativeFormat) is { } bytes)
                return bytes;
            if (TryGetString(nativeFormat) is { } stringValue)
                return Encoding.Unicode.GetBytes(stringValue);
            return null;
        }

        return null;
    }

    private Bitmap? TryGetBitmap(string nativeFormat)
    {
        using var bytes = _session.GetItemValueAsBytes(_itemIndex, nativeFormat);
        return bytes is null ? null : new Bitmap(new MemoryStream(bytes.Bytes, false));
    }
    
    private string? TryGetString(string nativeFormat)
    {
        using var text = _session.GetItemValueAsString(_itemIndex, nativeFormat);
        return text?.String;
    }

    private IStorageItem? TryGetFile(string nativeFormat)
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

    private byte[]? TryGetBytes(string nativeFormat)
    {
        using var bytes = _session.GetItemValueAsBytes(_itemIndex, nativeFormat);
        return bytes?.Bytes;
    }
}
