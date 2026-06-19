using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient.Clipboard;
using Avalonia.X11.Clipboard;

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
    private IReadOnlyList<IDataTransferItem>? _syncItems;
    private IReadOnlyList<IAsyncDataTransferItem>? _asyncItems;

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

    /// <summary>
    /// Pre-reads the file list asynchronously and builds the item layout. Used by the clipboard read
    /// path so the (cross-process) pipe read doesn't block the caller's thread.
    /// </summary>
    internal async Task PreloadAsync()
    {
        if (_syncItems != null)
            return;
        SetItems(BuildItems(await ReadFilesAsync().ConfigureAwait(false)));
    }

    private void EnsureItemsBlocking()
    {
        if (_syncItems != null)
            return;
        // Synchronous IDataTransfer contract — blocking here is allowed (DnD reads this path at drop
        // on the UI thread). The offer read happens on a pool thread (the cookie posts to the Wayland
        // thread, continuations use ConfigureAwait(false)), so it can't deadlock. wl_data_offer
        // transfers are always cross-process — in-process drags use the original IDataTransfer.
        SetItems(BuildItems(ReadFilesAsync().GetAwaiter().GetResult()));
    }

    private void SetItems(List<IDataTransferItem> items)
    {
        _syncItems = items;
        _asyncItems = items.ConvertAll(static i => (IAsyncDataTransferItem)i);
    }

    /// <summary>
    /// Builds the item layout: one item per file (each carrying a single <see cref="IStorageItem"/>,
    /// matching <see cref="DataFormat.File"/>'s single-value contract) plus one shared item for all
    /// remaining formats. Mirrors the other platforms so file copy/paste and drag-drop work.
    /// </summary>
    private List<IDataTransferItem> BuildItems(IReadOnlyList<IStorageItem>? files)
    {
        var (formats, formatToMime) = BuildFormats();
        var items = new List<IDataTransferItem>();
        List<DataFormat>? nonFileFormats = null;

        foreach (var format in formats)
        {
            if (DataFormat.File.Equals(format))
            {
                if (files != null)
                    foreach (var file in files)
                        items.Add(PlatformDataTransferItem.Create(DataFormat.File, file));
            }
            else
                (nonFileFormats ??= []).Add(format);
        }

        if (nonFileFormats is not null)
            items.Add(new WaylandDataTransferItem(nonFileFormats.ToArray(), formatToMime, _offerCookie));

        return items;
    }

    /// <summary>Reads and parses the <c>text/uri-list</c> into storage items, or null if no File format.</summary>
    private async Task<IStorageItem[]?> ReadFilesAsync()
    {
        if (!BuildFormats().formatToMime.TryGetValue(DataFormat.File, out var mime))
            return null;
        var bytes = await WaylandDataTransferItem.ReadRawBytesAsync(_offerCookie, mime).ConfigureAwait(false);
        return bytes is { Length: > 0 } ? ClipboardUriListHelper.Utf8BytesToFileUriList(bytes) : null;
    }

    IReadOnlyList<DataFormat> IDataTransfer.Formats => GetFormats();
    IReadOnlyList<DataFormat> IAsyncDataTransfer.Formats => GetFormats();

    // Synchronous interface: may block to resolve the file list (allowed by the IDataTransfer contract).
    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
    {
        get { EnsureItemsBlocking(); return _syncItems!; }
    }

    // The fallback here isn't supposed to be called since clipboard calls PreloadAsync, but we do a non-blocking
    // fallback just in case
    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => _asyncItems ?? BuildItems(null).ConvertAll(static i => (IAsyncDataTransferItem)i);

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
    /// Core async implementation shared by sync and async paths. Returns raw bytes; callers are
    /// responsible for calling <see cref="ConvertData"/> on the correct thread.
    /// </summary>
    private Task<byte[]?> TryGetRawCoreAsync(DataFormat format)
    {
        var mimeType = FindMimeType(format);
        return mimeType == null ? Task.FromResult<byte[]?>(null) : ReadRawBytesAsync(_offerCookie, mimeType);
    }

    /// <summary>
    /// Reads the raw bytes offered for a MIME type through a pipe. Uses ConfigureAwait(false) so a
    /// synchronous caller (<see cref="TryGetRaw"/>) doesn't deadlock on the dispatcher context.
    /// </summary>
    internal static async Task<byte[]?> ReadRawBytesAsync(WaylandOfferCookie offerCookie, string mimeType)
    {
        // Send cookie + MIME to Wayland thread; cookie.TryReceiveAsync posts and returns fd.
        var readFd = await offerCookie.TryReceiveAsync(mimeType).ConfigureAwait(false);
        if (readFd < 0)
            return null;

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

        // Files are never read through a WaylandDataTransferItem: WaylandDataTransfer splits them
        // into one PlatformDataTransferItem per file (DataFormat.File is a single-value format).

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
}
