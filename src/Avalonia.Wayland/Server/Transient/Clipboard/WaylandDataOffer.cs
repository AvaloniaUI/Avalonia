using System;
using System.Collections.Generic;
using Avalonia.Input;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient.Clipboard;

/// <summary>
/// Wraps a <see cref="WlDataOffer"/> received from the compositor.
/// Collects offered MIME types as they arrive and provides access to the offer's data.
/// Lives on the Wayland thread.
/// </summary>
class WaylandDataOffer : IDisposable
{
    private readonly WlDataOffer _offer;
    private readonly List<string> _mimeTypes = new();

    public IReadOnlyList<string> MimeTypes => _mimeTypes;

    /// <summary>
    /// Source-advertised DnD actions (set by <c>source_actions</c> event).
    /// </summary>
    public WlDataDeviceManager.DndActionEnum SourceActions { get; internal set; }

    /// <summary>
    /// Compositor-negotiated DnD action (set by <c>action</c> event).
    /// </summary>
    public WlDataDeviceManager.DndActionEnum NegotiatedAction { get; internal set; }

    public WaylandDataOffer(WlDataOffer offer)
    {
        _offer = offer;
    }

    internal void AddMimeType(string mimeType)
    {
        _mimeTypes.Add(mimeType);
    }

    /// <summary>
    /// Checks whether the offer contains the given MIME type.
    /// </summary>
    public bool HasMimeType(string mimeType)
    {
        foreach (var m in _mimeTypes)
        {
            if (m == mimeType)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Calls <c>wl_data_offer.receive(mimeType, fd)</c> using a pipe.
    /// Returns the read-end file descriptor. The caller must close it after reading.
    /// Returns -1 on failure.
    /// </summary>
    public unsafe int Receive(string mimeType)
    {
        int* fds = stackalloc int[2];
        if (Server.Interop.UnsafeNativeMethods.pipe2(fds,
                Server.Interop.UnsafeNativeMethods.O_CLOEXEC) != 0)
            return -1;

        _offer.Receive(mimeType, fds[1]);
        Server.Interop.UnsafeNativeMethods.close(fds[1]);
        return fds[0];
    }

    /// <summary>
    /// Sets accepted DnD actions on this offer.
    /// </summary>
    public void SetActions(WlDataDeviceManager.DndActionEnum dndActions,
        WlDataDeviceManager.DndActionEnum preferredAction)
    {
        _offer.SetActions(dndActions, preferredAction);
    }

    /// <summary>
    /// Accepts the given MIME type for DnD.
    /// </summary>
    public void Accept(uint serial, string? mimeType)
    {
        _offer.Accept(serial, mimeType!);
    }

    /// <summary>
    /// Signals that the DnD operation is complete.
    /// </summary>
    public void Finish()
    {
        _offer.Finish();
    }

    public void Dispose()
    {
        _offer.Destroy();
    }
}

/// <summary>
/// Listener for <see cref="WlDataOffer"/> events.
/// Collects MIME types and forwards DnD action changes.
/// </summary>
class WaylandDataOfferListener : WlDataOffer.Listener
{
    private WaylandDataOffer? _wrapper;

    /// <summary>
    /// Sets the wrapper that MIME types should be added to.
    /// Must be called immediately after the offer is created.
    /// </summary>
    internal void SetWrapper(WaylandDataOffer wrapper) => _wrapper = wrapper;

    protected override void Offer(WlDataOffer eventSender, string mimeType)
    {
        _wrapper?.AddMimeType(mimeType);
    }

    protected override void SourceActions(WlDataOffer eventSender, WlDataDeviceManager.DndActionEnum sourceActions)
    {
        if (_wrapper != null)
            _wrapper.SourceActions = sourceActions;
    }

    protected override void Action(WlDataOffer eventSender, WlDataDeviceManager.DndActionEnum dndAction)
    {
        if (_wrapper != null)
            _wrapper.NegotiatedAction = dndAction;
    }
}
