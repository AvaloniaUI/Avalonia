using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Wayland.Server.Persistent;
using NWayland;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient.Clipboard;

/// <summary>
/// Per-seat <see cref="WlDataDevice"/> wrapper. Handles clipboard selection events and DnD events.
/// Lives on the Wayland thread.
/// </summary>
class WaylandDataDevice : IDisposable
{
    private readonly WlDataDevice _device;
    private readonly WaylandInputDispatcher _dispatcher;
    private readonly WlDisplay _display;
    private readonly WaylandWorker _worker;
    private readonly Func<RawInputModifiers> _getKeyboardModifiers;

    /// <summary>
    /// The most recently announced data offer (not yet assigned to selection or DnD).
    /// Set by the <c>data_offer</c> event, consumed by <c>selection</c> or <c>enter</c>.
    /// </summary>
    private WaylandDataOffer? _pendingOffer;

    /// <summary>
    /// Cookie for the current selection offer.
    /// Encapsulates the offer and validates staleness on reads.
    /// </summary>
    private WaylandOfferCookie? _selectionCookie;

    // DnD session state
    private WaylandDataOffer? _dndOffer;
    private WaylandOfferCookie? _dndCookie;
    private uint _dndSerial;
    private WSurfaceEventSinkProxy? _dndSink;
    private Point _dndPosition;

    // Pending drops: offer+cookie moved here on drop(), consumed by FinishDnd/CancelDnd from UI thread
    private readonly record struct PendingDrop(WaylandDataOffer Offer, WaylandOfferCookie Cookie);
    private readonly List<PendingDrop> _pendingDropOperations = new();

    /// <summary>
    /// The current clipboard selection offer.
    /// Valid until a new <c>selection</c> event replaces it or keyboard focus is lost.
    /// </summary>
    public WaylandDataOffer? SelectionOffer { get; private set; }

    /// <summary>
    /// The currently active source we set via <c>set_selection</c>.
    /// Null if we don't own the clipboard.
    /// </summary>
    public WaylandDataSource? ActiveSource { get; set; }

    /// <summary>
    /// The serial from the most recent input event that can authorize <c>set_selection</c>
    /// (keyboard enter/key, pointer button, touch down).
    /// </summary>
    public uint LastInputSerial { get; set; }

    public WaylandDataDevice(WlDataDevice device, WaylandInputDispatcher dispatcher,
        WlSeat wlSeat, WlDisplay display, WaylandWorker worker, Func<RawInputModifiers> getKeyboardModifiers)
    {
        _device = device;
        _dispatcher = dispatcher;
        _display = display;
        _worker = worker;
        _getKeyboardModifiers = getKeyboardModifiers;
    }

    /// <summary>
    /// Sets the clipboard selection to the given source.
    /// </summary>
    public void SetSelection(WaylandDataSource? source)
    {
        ActiveSource?.Dispose();
        ActiveSource = source;
        _device.SetSelection(source?.WlSource!, LastInputSerial);
    }

    /// <summary>
    /// Gets the cookie and MIME type list for the current selection offer.
    /// Returns <c>(null, null)</c> if there is no valid selection.
    /// Called from the Wayland thread on behalf of UI-thread queries.
    /// </summary>
    public (WaylandOfferCookie? Cookie, string[]? MimeTypes) GetSelectionInfo()
    {
        if (SelectionOffer == null || SelectionOffer.MimeTypes.Count == 0)
            return (null, null);

        var mimeTypes = new string[SelectionOffer.MimeTypes.Count];
        for (var i = 0; i < SelectionOffer.MimeTypes.Count; i++)
            mimeTypes[i] = SelectionOffer.MimeTypes[i];

        return (_selectionCookie, mimeTypes);
    }

    internal void OnDataOffer(WlDataOffer wlOffer, WaylandDataOfferListener listener)
    {
        var offer = new WaylandDataOffer(wlOffer);
        listener.SetWrapper(offer);
        _pendingOffer = offer;
    }

    internal void OnSelection(WlDataOffer? wlOffer)
    {
        _selectionCookie?.Invalidate();
        SelectionOffer?.Dispose();
        _selectionCookie = null;

        if (wlOffer != null && _pendingOffer != null)
        {
            SelectionOffer = _pendingOffer;
            _selectionCookie = new WaylandOfferCookie(this, _pendingOffer, _worker);
            _pendingOffer = null;
        }
        else
        {
            SelectionOffer = null;
            _pendingOffer?.Dispose();
            _pendingOffer = null;
        }
    }
    
    internal void OnDndEnter(uint serial, WlSurface? surface, double x, double y)
    {
        var offer = _pendingOffer;
        _pendingOffer = null;

        if (offer == null)
            return;

        var shellSurface = WaylandInputDispatcher.FindSurfaceForWlSurface(surface);
        if (shellSurface == null)
        {
            offer.Dispose();
            return;
        }

        // Clean up any lingering DnD session
        CleanupDndState();

        _dndOffer = offer;
        _dndCookie = new WaylandOfferCookie(this, offer, _worker);
        _dndSerial = serial;
        _dndSink = shellSurface.EventSink;
        _dndPosition = new Point(x, y);

        var sourceEffects = ActionsToEffects(offer.SourceActions);

        var mimeTypes = new string[offer.MimeTypes.Count];
        for (var i = 0; i < offer.MimeTypes.Count; i++)
            mimeTypes[i] = offer.MimeTypes[i];

        var modifiers = _getKeyboardModifiers();

        _dndSink.OnDragEnter(_dndPosition, mimeTypes, _dndCookie!, sourceEffects, modifiers);
    }

    internal void OnDndMotion(double x, double y)
    {
        _dndPosition = new Point(x, y);
        _dndSink?.OnDragMotion(_dndPosition, _getKeyboardModifiers());
    }

    internal void OnDndLeave()
    {
        // If active session exists (no preceding drop), notify and clean up.
        // After drop(), _dndSink is already null so this is a no-op.
        _dndSink?.OnDragLeave();
        CleanupDndState();
    }

    internal void OnDndDrop()
    {
        // Move offer+cookie to pending list so they survive CleanupDndState.
        // The UI thread will consume them via FinishDnd/CancelDnd.
        if (_dndOffer != null && _dndCookie != null)
        {
            _pendingDropOperations.Add(new PendingDrop(_dndOffer, _dndCookie));
            _dndOffer = null;
            _dndCookie = null;
        }

        _dndSink?.OnDrop(_dndPosition, _getKeyboardModifiers());

        // Reset the active session state; pending drops keep the device alive.
        _dndSink = null;
        _dndSerial = 0;
        _dndPosition = default;
    }

    /// <summary>
    /// Called from the UI thread (via PostOob) after processing a DragEnter or DragOver event.
    /// Updates the offer's accepted action and MIME type based on the UI's response.
    /// </summary>
    internal void UpdateDndFeedback(DragDropEffects effects)
    {
        if (_dndOffer == null)
            return;

        var (actions, preferred) = EffectsToActionsWithPreferred(effects);
        _dndOffer.SetActions(actions, preferred);

        if (effects == DragDropEffects.None) 
            _dndOffer.Accept(_dndSerial, null);
        else
        {
            foreach(var mime in _dndOffer.MimeTypes)
                _dndOffer.Accept(_dndSerial, mime);
        }
    }

    /// <summary>
    /// Called from the UI thread (via PostOob) after a successful drop.
    /// Signals to the source that the transfer is complete.
    /// </summary>
    internal void FinishDnd()
    {
        if (_pendingDropOperations.Count > 0)
        {
            var pending = _pendingDropOperations[0];
            _pendingDropOperations.RemoveAt(0);
            pending.Offer.Finish();
            pending.Cookie.Invalidate();
            pending.Offer.Dispose();
        }
    }

    private void CleanupDndState()
    {
        _dndCookie?.Invalidate();
        _dndCookie = null;
        _dndOffer?.Dispose();
        _dndOffer = null;
        _dndSink = null;
        _dndSerial = 0;
        _dndPosition = default;
    }
    
    /// <summary>
    /// Starts a drag-and-drop operation from the given origin surface using the
    /// trigger event's serial. Called from the Wayland thread.
    /// </summary>
    internal bool StartDrag(WaylandDataSource source, object? platformCookie,
        WlDataDeviceManager.DndActionEnum allowedActions)
    {
        if (platformCookie is not WaylandInputEventCookie cookie
            || !cookie.TryConsume(_display, out _, out var serial))
            return false;

        source.SetActions(allowedActions);

        // Find the origin surface — the surface that has the implicit pointer grab
        var originSurface = _dispatcher.FindOriginSurface();
        if (originSurface == null)
            return false;

        _device.StartDrag(source.WlSource, originSurface, null!, serial);
        return true;
    }

    internal static DragDropEffects ActionsToEffects(WlDataDeviceManager.DndActionEnum actions)
    {
        var effects = DragDropEffects.None;
        if (actions.HasFlag(WlDataDeviceManager.DndActionEnum.Copy))
            effects |= DragDropEffects.Copy;
        if (actions.HasFlag(WlDataDeviceManager.DndActionEnum.Move))
            effects |= DragDropEffects.Move;
        return effects;
    }

    internal static WlDataDeviceManager.DndActionEnum EffectsToActions(DragDropEffects effects)
    {
        var actions = WlDataDeviceManager.DndActionEnum.None;
        if (effects.HasFlag(DragDropEffects.Copy))
            actions |= WlDataDeviceManager.DndActionEnum.Copy;
        if (effects.HasFlag(DragDropEffects.Move))
            actions |= WlDataDeviceManager.DndActionEnum.Move;
        return actions;
    }

    private static (WlDataDeviceManager.DndActionEnum Actions, WlDataDeviceManager.DndActionEnum Preferred)
        EffectsToActionsWithPreferred(DragDropEffects effects)
    {
        var actions = WlDataDeviceManager.DndActionEnum.None;
        if (effects.HasFlag(DragDropEffects.Copy))
            actions |= WlDataDeviceManager.DndActionEnum.Copy;
        if (effects.HasFlag(DragDropEffects.Move))
            actions |= WlDataDeviceManager.DndActionEnum.Move;

        // Prefer copy if both are available
        var preferred = actions.HasFlag(WlDataDeviceManager.DndActionEnum.Copy)
            ? WlDataDeviceManager.DndActionEnum.Copy
            : actions;

        return (actions, preferred);
    }

    public void Dispose()
    {
        _selectionCookie?.Invalidate();
        _selectionCookie = null;
        SelectionOffer?.Dispose();
        SelectionOffer = null;
        CleanupDndState();
        foreach (var pending in _pendingDropOperations)
        {
            pending.Cookie.Invalidate();
            pending.Offer.Dispose();
        }
        _pendingDropOperations.Clear();
        _pendingOffer?.Dispose();
        _pendingOffer = null;
        ActiveSource?.Dispose();
        ActiveSource = null;
        _device.Release();
    }
}

class WaylandDataDeviceListener : WlDataDevice.Listener
{
    private WaylandDataDevice? _wrapper;

    internal void SetWrapper(WaylandDataDevice wrapper) => _wrapper = wrapper;

    protected override void DataOffer(WlDataDevice eventSender, NewId<WlDataOffer, WlDataOffer.Listener> id)
    {
        var listener = new WaylandDataOfferListener();
        var wlOffer = id.GetAndConsume(listener);
        _wrapper?.OnDataOffer(wlOffer, listener);
    }

    protected override void Enter(WlDataDevice eventSender, uint serial, WlSurface? surface, WlFixed x, WlFixed y, WlDataOffer? offer)
    {
        _wrapper?.OnDndEnter(serial, surface, (double)x, (double)y);
    }

    protected override void Leave(WlDataDevice eventSender)
    {
        _wrapper?.OnDndLeave();
    }

    protected override void Motion(WlDataDevice eventSender, uint time, WlFixed x, WlFixed y)
    {
        _wrapper?.OnDndMotion((double)x, (double)y);
    }

    protected override void Drop(WlDataDevice eventSender)
    {
        _wrapper?.OnDndDrop();
    }

    protected override void Selection(WlDataDevice eventSender, WlDataOffer? offer)
    {
        _wrapper?.OnSelection(offer);
    }
}
