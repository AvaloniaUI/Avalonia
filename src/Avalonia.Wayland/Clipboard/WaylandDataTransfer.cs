using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient.Clipboard;

namespace Avalonia.Wayland.Clipboard;

/// <summary>
/// Data transfer backed by a Wayland <c>wl_data_offer</c>, implementing both sync and async interfaces.
/// The offer cookie is validated on each read to detect stale offers.
/// Reused by both clipboard and DnD.
/// </summary>
class WaylandDataTransfer : IDataTransfer, IAsyncDataTransfer
{
    private readonly string[] _mimeTypes;
    private readonly WaylandOfferCookie _offerCookie;
    private DataFormat[]? _formats;
    private Dictionary<DataFormat, string>? _formatToMime;
    private WaylandDataTransferItem[]? _items;

    public WaylandDataTransfer(string[] mimeTypes, WaylandOfferCookie offerCookie)
    {
        _mimeTypes = mimeTypes;
        _offerCookie = offerCookie;
    }

    private (DataFormat[] formats, Dictionary<DataFormat, string> formatToMime) BuildFormats()
    {
        if (_formats != null)
            return (_formats, _formatToMime!);

        var formatToMime = new Dictionary<DataFormat, string>();
        var formats = new List<DataFormat>();
        foreach (var mime in _mimeTypes)
        {
            var format = WaylandMimeMapper.FromMimeType(mime);
            if (format != null && formatToMime.TryAdd(format, mime))
                formats.Add(format);
        }
        _formats = formats.ToArray();
        _formatToMime = formatToMime;
        return (_formats, _formatToMime);
    }

    private DataFormat[] GetFormats() => BuildFormats().formats;

    private WaylandDataTransferItem[] GetItems()
    {
        var (formats, formatToMime) = BuildFormats();
        return _items ??= [new WaylandDataTransferItem(formats, formatToMime, _offerCookie)];
    }

    IReadOnlyList<DataFormat> IDataTransfer.Formats => GetFormats();
    IReadOnlyList<DataFormat> IAsyncDataTransfer.Formats => GetFormats();
    IReadOnlyList<IDataTransferItem> IDataTransfer.Items => GetItems();
    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items => GetItems();

    public void Dispose()
    {
        // The offer is owned by WaylandDataDevice — nothing to dispose here
    }
}

/// <summary>
/// A single data transfer item that reads from a Wayland data offer via pipe.
/// The offer cookie is sent back to the Wayland thread on each read;
/// the cookie validates the offer is still live and performs the receive internally.
/// Implements both <see cref="IDataTransferItem"/> (sync, blocks) and
/// <see cref="IAsyncDataTransferItem"/> (true async via pipe).
/// </summary>
class WaylandDataTransferItem : IDataTransferItem, IAsyncDataTransferItem
{
    private readonly DataFormat[] _formats;
    private readonly Dictionary<DataFormat, string> _formatToMime;
    private readonly WaylandOfferCookie _offerCookie;

    public WaylandDataTransferItem(DataFormat[] formats,
        Dictionary<DataFormat, string> formatToMime, WaylandOfferCookie offerCookie)
    {
        _formats = formats;
        _formatToMime = formatToMime;
        _offerCookie = offerCookie;
    }

    IReadOnlyList<DataFormat> IDataTransferItem.Formats => _formats;
    IReadOnlyList<DataFormat> IAsyncDataTransferItem.Formats => _formats;

    /// <summary>
    /// Synchronous read — blocks the calling thread while async I/O completes on pool thread.
    /// Used for DnD (sync data access during event handling).
    /// </summary>
    public object? TryGetRaw(DataFormat format)
    {
        if (Array.IndexOf(_formats, format) < 0)
            return null;
        var data = TryGetRawCoreAsync(format).GetAwaiter().GetResult();
        return data != null ? ConvertData(format, data) : null;
    }

    /// <summary>
    /// Asynchronous read — sends the offer cookie to the Wayland thread,
    /// which validates and creates a pipe; data is read on the current/pool thread.
    /// ConvertData runs on the dispatcher thread after resuming from the await.
    /// </summary>
    public async Task<object?> TryGetRawAsync(DataFormat format)
    {
        if (Array.IndexOf(_formats, format) < 0)
            return null;
        var data = await TryGetRawCoreAsync(format);
        return data != null ? ConvertData(format, data) : null;
    }

    /// <summary>
    /// Core async implementation shared by sync and async paths.
    /// Uses ConfigureAwait(false) to avoid capturing the dispatcher context,
    /// which would deadlock when called synchronously from <see cref="TryGetRaw"/>.
    /// Returns raw bytes; callers are responsible for calling ConvertData on the correct thread.
    /// </summary>
    private async Task<byte[]?> TryGetRawCoreAsync(DataFormat format)
    {
        var mimeType = FindMimeType(format);
        if (mimeType == null)
            return null;

        // Send cookie + MIME to Wayland thread; cookie.TryReceiveAsync posts and returns fd
        var readFd = await _offerCookie.TryReceiveAsync(mimeType).ConfigureAwait(false);

        if (readFd < 0)
            return null;

        // Read all data from the pipe
        try
        {
            await using var stream = new Pipe2Stream(readFd, PipeDirection.In);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms).ConfigureAwait(false);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Finds the MIME type to use for the given format.
    /// Uses the preserved mapping from the original offer first (round-trippable),
    /// then falls back to the mapper helper.
    /// </summary>
    private string? FindMimeType(DataFormat format)
    {
        if (_formatToMime.TryGetValue(format, out var mime))
            return mime;

        return null;
    }

    internal static object? ConvertData(DataFormat format, byte[] data)
    {
        if (data.Length == 0)
            return null;

        if (DataFormat.Text.Equals(format))
            return Encoding.UTF8.GetString(data);

        if (DataFormat.File.Equals(format))
            return Utf8BytesToFileUriList(data);

        if (DataFormat.Bitmap.Equals(format))
        {
            using var ms = new MemoryStream(data);
            return new Bitmap(ms);
        }

        if (format is DataFormat<string>)
            return Encoding.UTF8.GetString(data);

        if (format is DataFormat<byte[]>)
            return data;

        return null;
    }

    internal static IStorageItem[] Utf8BytesToFileUriList(byte[] utf8Bytes)
    {
        try
        {
            using var stream = new MemoryStream(utf8Bytes);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var items = new List<IStorageItem>();

            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;
                if (Uri.TryCreate(line.TrimEnd(), UriKind.Absolute, out var uri) &&
                    uri.IsFile &&
                    StorageProviderHelpers.TryCreateBclStorageItem(uri.LocalPath) is { } storageItem)
                {
                    items.Add(storageItem);
                }
            }

            return items.ToArray();
        }
        catch
        {
            return [];
        }
    }
}
