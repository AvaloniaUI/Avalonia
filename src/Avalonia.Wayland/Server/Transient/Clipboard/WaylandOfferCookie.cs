using System.Threading.Tasks;
using Avalonia.Input;

namespace Avalonia.Wayland.Server.Transient.Clipboard;

/// <summary>
/// Opaque token representing a specific <see cref="WaylandDataOffer"/> on the Wayland thread.
/// Passed to UI-thread code as an <c>object</c> reference.
/// <para>
/// For data reads the UI thread sends this cookie back to the Wayland thread
/// via <c>InvokeAsync</c>, and the cookie performs the receive internally.
/// For DnD sessions this cookie also carries feedback methods (<see cref="PostUpdateFeedback"/>,
/// <see cref="PostFinish"/> that route through <c>PostOob</c>.
/// </para>
/// All field access happens on the Wayland thread — the UI thread only holds a reference.
/// </summary>
class WaylandOfferCookie
{
    private readonly WaylandDataDevice _device;
    private readonly WaylandDataOffer _offer;
    private readonly WaylandWorker? _worker;
    private bool _invalidated;

    internal WaylandOfferCookie(WaylandDataDevice device, WaylandDataOffer offer, WaylandWorker? worker)
    {
        _device = device;
        _offer = offer;
        _worker = worker;
    }

    /// <summary>
    /// Called from the Wayland thread when this offer is superseded by a new selection
    /// or when the device is disposed.
    /// </summary>
    internal void Invalidate() => _invalidated = true;

    /// <summary>
    /// Attempts to receive data for a MIME type from the underlying offer.
    /// Returns the read-end pipe fd, or -1 if the offer has been invalidated.
    /// Must be called on the Wayland thread.
    /// </summary>
    internal int TryReceive(string mimeType)
    {
        if (_invalidated)
            return -1;
        return _offer.Receive(mimeType);
    }

    /// <summary>
    /// Asynchronously receives data for a MIME type by posting to the Wayland thread.
    /// Returns the read-end pipe fd, or -1 if the offer has been invalidated.
    /// Called from the UI thread.
    /// </summary>
    internal Task<int> TryReceiveAsync(string mimeType)
    {
        if (_worker is null)
            return Task.FromResult(-1);
        return _worker.InvokeAsync(() => TryReceive(mimeType));
    }

    /// <summary>
    /// Posts a DnD feedback update to the Wayland thread.
    /// Called from UI-thread code after processing a DragEnter or DragOver event.
    /// </summary>
    internal void PostUpdateFeedback(DragDropEffects effects)
    {
        _worker?.PostOob(() =>
        {
            if (!_invalidated)
                _device.UpdateDndFeedback(effects);
        });
    }

    /// <summary>
    /// Posts a DnD finish signal to the Wayland thread.
    /// Called from UI-thread code after a successful drop.
    /// </summary>
    internal void PostFinish()
    {
        _worker?.PostOob(() =>
        {
            if (!_invalidated)
                _device.FinishDnd();
        });
    }
}
