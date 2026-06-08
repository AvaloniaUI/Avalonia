using System;
using NWayland;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient.Clipboard;

/// <summary>
/// Wraps a <see cref="WlDataSource"/> created by us (for clipboard set or DnD start).
/// Handles <c>send</c> (compositor requests data) and <c>cancelled</c> (we lost ownership) events.
/// Lives on the Wayland thread.
/// </summary>
class WaylandDataSource : IDisposable
{
    private readonly WlDataSource _source;
    private bool _destroyed;

    /// <summary>
    /// Called on the Wayland thread when the compositor requests data for a MIME type.
    /// The callback receives (mimeType, fd) and must write data and close the fd.
    /// </summary>
    public Action<string, int>? OnSend { get; set; }

    /// <summary>
    /// Called on the Wayland thread when this source has been cancelled
    /// (another client took clipboard ownership, or DnD was cancelled).
    /// </summary>
    public Action? OnCancelled { get; set; }

    /// <summary>
    /// Called when DnD drop has been performed by the user.
    /// </summary>
    public Action? OnDndDropPerformed { get; set; }

    /// <summary>
    /// Called when the DnD operation has been completed by the destination.
    /// </summary>
    public Action? OnDndFinished { get; set; }

    /// <summary>
    /// Called when the compositor negotiates a DnD action.
    /// </summary>
    public Action<WlDataDeviceManager.DndActionEnum>? OnAction { get; set; }

    public WaylandDataSource(WlDataSource source)
    {
        _source = source;
    }

    /// <summary>
    /// Advertises a MIME type that this source can provide.
    /// Must be called before <c>set_selection</c> or <c>start_drag</c>.
    /// </summary>
    public void Offer(string mimeType)
    {
        _source.Offer(mimeType);
    }

    /// <summary>
    /// Sets the DnD actions supported by this source.
    /// </summary>
    public void SetActions(WlDataDeviceManager.DndActionEnum actions)
    {
        _source.SetActions(actions);
    }

    public WlDataSource WlSource => _source;

    public void Dispose()
    {
        if (!_destroyed)
        {
            _destroyed = true;
            _source.Destroy();
        }
    }
}

class WaylandDataSourceListener : WlDataSource.Listener
{
    private WaylandDataSource? _wrapper;

    internal void SetWrapper(WaylandDataSource wrapper) => _wrapper = wrapper;

    protected override void Send(WlDataSource eventSender, string mimeType, WaylandFd fd)
    {
        _wrapper?.OnSend?.Invoke(mimeType, fd.Consume());
    }

    protected override void Cancelled(WlDataSource eventSender)
    {
        _wrapper?.OnCancelled?.Invoke();
    }

    protected override void Target(WlDataSource eventSender, string? mimeType)
    {
        // DnD target notification — not needed for clipboard
    }

    protected override void DndDropPerformed(WlDataSource eventSender)
    {
        _wrapper?.OnDndDropPerformed?.Invoke();
    }

    protected override void DndFinished(WlDataSource eventSender)
    {
        _wrapper?.OnDndFinished?.Invoke();
    }

    protected override void Action(WlDataSource eventSender, WlDataDeviceManager.DndActionEnum dndAction)
    {
        _wrapper?.OnAction?.Invoke(dndAction);
    }
}
