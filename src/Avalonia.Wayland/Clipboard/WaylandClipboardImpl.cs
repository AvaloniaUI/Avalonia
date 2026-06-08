using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Transient.Clipboard;

namespace Avalonia.Wayland.Clipboard;

/// <summary>
/// Implements <see cref="IOwnedClipboardImpl"/> for the Wayland backend.
/// All Wayland protocol calls happen on the Wayland thread via <see cref="WaylandWorker"/>.
/// Data I/O (pipe reads/writes) happens on pool threads using .NET async APIs.
/// No shared mutable state between threads — all cross-thread communication via messaging.
/// </summary>
class WaylandClipboardImpl : IOwnedClipboardImpl
{
    private readonly WaylandWorker _worker;

    public WaylandClipboardImpl(WaylandWorker worker)
    {
        _worker = worker;
    }

    public async Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        var info = await _worker.InvokeAsync(() =>
        {
            var device = _worker.Globals?.InputDispatcher.GetDataDevice();
            if (device == null)
                return ((Server.Transient.Clipboard.WaylandOfferCookie?)null, (string[]?)null);
            return device.GetSelectionInfo();
        });

        if (info.Item1 == null || info.Item2 == null || info.Item2.Length == 0)
            return null;

        return new WaylandDataTransfer(info.Item2, info.Item1);
    }

    public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        var outgoing = new WaylandOutgoingTransfer(dataTransfer);
        await outgoing.SetAsSelection(_worker);
    }

    public Task ClearAsync() => SetDataAsync(new DataTransfer());

    public async Task<bool> IsCurrentOwnerAsync()
    {
        return await _worker.InvokeAsync(() =>
        {
            var device = _worker.Globals?.InputDispatcher.GetDataDevice();
            return device?.ActiveSource != null;
        });
    }

    /// <summary>
    /// Serializes data from an <see cref="IAsyncDataTransfer"/> for a specific MIME type.
    /// Runs on a pool thread.
    /// </summary>
    internal static async Task<byte[]?> SerializeForMimeAsync(IAsyncDataTransfer transfer, string mimeType)
    {
        var format = WaylandMimeMapper.FromMimeType(mimeType);
        if (format == null)
            return null;

        foreach (var item in transfer.Items)
        {
            var value = await item.TryGetRawAsync(format);
            if (value == null)
                continue;

            if (DataFormat.Text.Equals(format))
            {
                if (value is string text)
                    return Encoding.UTF8.GetBytes(text);
            }
            else if (DataFormat.File.Equals(format))
            {
                if (value is IStorageItem storageItem)
                    return Encoding.UTF8.GetBytes(storageItem.Path.AbsoluteUri + "\r\n");
                if (value is IEnumerable<IStorageItem> items)
                {
                    var sb = new StringBuilder();
                    foreach (var si in items)
                    {
                        sb.Append(si.Path.AbsoluteUri);
                        sb.Append("\r\n");
                    }
                    return Encoding.UTF8.GetBytes(sb.ToString());
                }
            }
            else if (DataFormat.Bitmap.Equals(format))
            {
                if (value is Bitmap bitmap)
                {
                    using var ms = new MemoryStream();
                    bitmap.Save(ms);
                    return ms.ToArray();
                }
            }
            else if (value is byte[] bytes)
            {
                return bytes;
            }
            else if (value is string strValue)
            {
                return Encoding.UTF8.GetBytes(strValue);
            }
        }

        return null;
    }
}
