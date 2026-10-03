using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding.Compositor;

namespace Avalonia.Wayland.Embedding.Hosting;

/// <summary>
/// Hosts one embedded toolkit surface tree. Receives freshly-allocated bitmaps (ownership transferred) keyed
/// by surface id from compositor-thread snapshots, draws them in snapshot order clipped to the toplevel
/// geometry, and after <see cref="Render"/> posts the rendered surface ids back so the compositor fires the
/// deferred frame callbacks (the present-pace throttle). Holds no shared mutable state with the compositor.
/// </summary>
public class WaylandSubcompositorControlHost : Control
{
    private readonly Dictionary<uint, Bitmap> _bitmaps = new();
    private IReadOnlyList<SurfaceDrawItem> _drawOrder = Array.Empty<SurfaceDrawItem>();
    private double _clipWidth, _clipHeight;
    private uint[] _pendingFrameIds = Array.Empty<uint>();
    private bool _stopped;
    private int _reportedScale;
    private int _lastConfiguredWidth = -1;
    private int _lastConfiguredHeight = -1;
    private bool _pointerEntered;
    private TopLevel? _scaleHost;
    private bool _activeReported;
    private bool _lastActive;
    // Scenario 5: an Avalonia content host (WaylandSubcompositorAvaloniaContentHost) overlays its content into
    // this host at a glue-driven rect, rendered by Avalonia on top of the embedded toolkit bitmaps.
    private Control? _contentOverlay;
    private Rect _contentRect;
    private Rect? _contentClip;
    // wl_pointer.set_cursor: the client's custom cursor image applied to this host, kept so it can be disposed.
    private Avalonia.Input.Cursor? _embeddedCursor;
    private Bitmap? _embeddedCursorBitmap;

    /// <summary>Opaque compositor host id this control renders. Set by the hosting registry; 0 = unassigned.</summary>
    internal uint HostId { get; set; }

    /// <summary>The embedded toplevel surface's wayland object id (matches the client's <c>wl_proxy_get_id</c>), set
    /// at map. Lets the resize flush tell the toolkit glue which of its windows resized with no client round-trip.</summary>
    internal uint SurfaceObjectId { get; set; }

    /// <summary>The embedding connection's ticket (0 if none), scoping <see cref="SurfaceObjectId"/> to its connection.</summary>
    internal uint ConnectionTicket { get; set; }

    /// <summary>The last logical size (DIPs) this host arranged the embedded toplevel to — the size the glue applies
    /// to its window during the flush. 0 until first arranged after map.</summary>
    internal int ArrangedWidth => _lastConfiguredWidth > 0 ? _lastConfiguredWidth : 0;
    internal int ArrangedHeight => _lastConfiguredHeight > 0 ? _lastConfiguredHeight : 0;

    // Bridges Avalonia's IME to the embedded toolkit's zwp_text_input_v3 (preedit forward + caret-rect reverse).
    private readonly EmbeddedTextInputMethodClient _imClient;

    static WaylandSubcompositorControlHost()
    {
        // Offer our IME bridge as the focused element's text-input client, so the OS IME composes into the
        // embedded content (composition forwarded to the toolkit as preedit_string).
        TextInputMethodClientRequestedEvent.AddClassHandler<WaylandSubcompositorControlHost>(
            (host, e) => e.Client = host._imClient);
    }

    public WaylandSubcompositorControlHost()
    {
        Focusable = true; // accept keyboard focus so we can forward key events to the embedded client
        _imClient = new EmbeddedTextInputMethodClient(this);
    }

    /// <summary>Inspection hook for tests: the IME bridge this host offers to Avalonia's IME framework.</summary>
    internal EmbeddedTextInputMethodClient ImeClient => _imClient;

    /// <summary>Inspection hook for tests: the per-surface bitmaps currently held for drawing.</summary>
    internal IReadOnlyDictionary<uint, Bitmap> Bitmaps => _bitmaps;

    // ── embedded-window properties surfaced to layout (see "Layout integration" in 40-public-api.md) ──
    /// <summary>From <c>xdg_toplevel.set_min_size</c> (DIPs); null if unset.</summary>
    public Size? EmbeddedMinSize { get; private set; }
    /// <summary>From <c>xdg_toplevel.set_max_size</c> (DIPs); null if unset.</summary>
    public Size? EmbeddedMaxSize { get; private set; }
    /// <summary>Ignore the embedded min/max in <see cref="MeasureOverride"/> when true.</summary>
    public bool SuppressSizeConstraints { get; set; }

    /// <summary>
    /// When true (default), the embedded surface tree is stretched to fill this control's allocation, so a stale
    /// buffer is shown scaled during a resize round-trip (the X11-style "wiggle"). Set false for a frame-perfect
    /// wrapper (e.g. a toplevel window host): the buffer is then drawn 1:1 at the top-left and clipped, so a
    /// not-yet-resized frame reveals a one-frame edge gap instead of scale-distorting. The synchronous resize
    /// flush keeps the buffer in lock-step with the allocation, so in steady state neither path scales.
    /// </summary>
    public bool StretchContent { get; set; } = true;

    public string? EmbeddedTitle { get; private set; }
    public string? EmbeddedAppId { get; private set; }
    public bool IsEmbeddedSurfaceMapped { get; private set; }

    public event EventHandler? EmbeddedSurfaceMapped;
    public event EventHandler? EmbeddedSurfaceUnmapped;
    public event EventHandler? EmbeddedSizeConstraintsChanged;
    public event EventHandler? EmbeddedTitleChanged;

    /// <summary>
    /// Send xdg_toplevel.close — the compositor→client "user wants to close" hint. The client decides; it
    /// closes by destroying its surface (→ <see cref="EmbeddedSurfaceUnmapped"/>). For scenario-2 auto-windows
    /// this is wired from the Avalonia <c>Window.Closing</c> event. There is no client→compositor close request.
    /// </summary>
    public void RequestClose()
    {
        if (HostId != 0)
            WaylandEmbeddingSubcompositor.Api.CloseToplevel(HostId);
    }

    /// <summary>
    /// Ask the embedded client to re-lay-out at <paramref name="width"/>×<paramref name="height"/> DIPs via the
    /// configure handshake (carrying the current state, e.g. <c>activated</c>). For scenario-1 embedding the glue
    /// owns sizing, so it drives this (e.g. a resize handle); scenario-2 auto-windows configure from window resize.
    /// </summary>
    public void RequestResize(int width, int height)
    {
        if (HostId != 0 && width > 0 && height > 0)
            WaylandEmbeddingSubcompositor.Api.ConfigureToplevel(HostId, width, height);
    }

    /// <summary>
    /// Scenario 1: mint an opaque token bound to this (pre-created) host and round-trip the compositor so the
    /// token is live — and this control bound to its host id — before returning. Hand the token to the toolkit,
    /// which sends <c>avalonia_embed.embed_toplevel(wl_surface, token)</c>; its toplevel then renders here
    /// instead of in an auto-hosted window.
    /// </summary>
    public string GetEmbeddingToken()
    {
        Dispatcher.UIThread.VerifyAccess();
        var token = Guid.NewGuid().ToString("N");
        // Register the token and bind this control to the host id embed_toplevel(token) will resolve to, before returning.
        HostId = WaylandEmbeddingSubcompositor.RegisterEmbedToken(token);
        WaylandHosting.RegisterHost(HostId, this);
        _embeddingToken = token;
        return token;
    }

    private string? _embeddingToken;

    /// <summary>
    /// Scenario 1 glue helper: emit <c>avalonia_embed.embed_toplevel(wlSurface, token)</c> over <paramref name="connection"/>
    /// (the toolkit's own connection, with the embedder bound once), so the toolkit's toplevel renders into THIS control.
    /// Pass the toplevel's <c>wl_surface*</c> (e.g. GTK's <c>gdk_wayland_window_get_wl_surface</c>). Mints an embedding
    /// token if one wasn't already created. Returns on <c>bound</c>, throws on <c>rejected</c>.
    /// </summary>
    public void AttachClientSurface(WaylandEmbedderConnection connection, IntPtr wlSurfacePtr)
    {
        Dispatcher.UIThread.VerifyAccess();
        var token = _embeddingToken ?? GetEmbeddingToken();
        var outcome = connection.EmbedToplevel(wlSurfacePtr, token);
        if (outcome == EmbedOutcome.Bound)
            _embeddingToken = null; // the token is one-shot compositor-side; a future embed mints a fresh one
        if (outcome != EmbedOutcome.Bound)
            throw new InvalidOperationException($"avalonia_embed.embed_toplevel did not bind ({outcome}).");
    }

    /// <summary>Compositor-applied metadata for the embedded toplevel (title/app_id and size constraints).</summary>
    internal void SetMetadata(string title, string appId, Size? minSize, Size? maxSize)
    {
        var titleChanged = EmbeddedTitle != title;
        var constraintsChanged = EmbeddedMinSize != minSize || EmbeddedMaxSize != maxSize;
        EmbeddedTitle = title;
        EmbeddedAppId = appId;
        EmbeddedMinSize = minSize;
        EmbeddedMaxSize = maxSize;
        if (titleChanged)
            EmbeddedTitleChanged?.Invoke(this, EventArgs.Empty);
        if (constraintsChanged)
        {
            InvalidateMeasure();
            EmbeddedSizeConstraintsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void NotifyMapped()
    {
        if (IsEmbeddedSurfaceMapped)
            return;
        IsEmbeddedSurfaceMapped = true;
        ReportPreferredScale();    // the toplevel now exists compositor-side, so the scale hint can reach it
        ReportActivation(force: true); // and the activation state (focus-independent — follows the host's Window)
        EmbeddedSurfaceMapped?.Invoke(this, EventArgs.Empty);
    }

    internal void ApplyCommit(SurfaceCommit commit)
    {
        if (_stopped)
        {
            foreach (var bmp in commit.NewBitmaps.Values)
                bmp.Dispose();
            return;
        }

        foreach (var (id, bmp) in commit.NewBitmaps)
        {
            if (_bitmaps.TryGetValue(id, out var old))
                old.Dispose();
            _bitmaps[id] = bmp;
        }

        _drawOrder = commit.DrawOrder;
        _clipWidth = commit.ClipWidth;
        _clipHeight = commit.ClipHeight;
        if (commit.FrameSurfaceIds.Count > 0)
            _pendingFrameIds = AppendFrameIds(_pendingFrameIds, commit.FrameSurfaceIds);

        InvalidateMeasure();
        InvalidateVisual();
    }

    internal void Stop()
    {
        _stopped = true;
        foreach (var bmp in _bitmaps.Values)
            bmp.Dispose();
        _bitmaps.Clear();
        _pendingFrameIds = Array.Empty<uint>();
        _drawOrder = Array.Empty<SurfaceDrawItem>();
        _clipWidth = _clipHeight = 0;
        // Drop per-mapping dedupe so a future mapping isn't suppressed by stale "last configured/reported" values.
        _lastConfiguredWidth = _lastConfiguredHeight = -1;
        _reportedScale = 0;
        _activeReported = false;
        _lastActive = false; // reset both halves of the activation dedupe so a remap re-reports cleanly
        _pointerEntered = false;
        _embeddedCursor?.Dispose();
        _embeddedCursor = null;
        _embeddedCursorBitmap?.Dispose();
        _embeddedCursorBitmap = null;
        Cursor = null;
        if (IsEmbeddedSurfaceMapped)
        {
            IsEmbeddedSurfaceMapped = false;
            EmbeddedSurfaceUnmapped?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Inspection hook for tests: the scenario-5 content overlay control, if any.</summary>
    internal Control? ContentOverlay => _contentOverlay;

    /// <summary>Inspection hook for tests: the custom cursor bitmap applied via wl_pointer.set_cursor, if any.</summary>
    internal Bitmap? EmbeddedCursorBitmap => _embeddedCursorBitmap;

    /// <summary>
    /// Scenario P3: apply the client's custom cursor image (wl_pointer.set_cursor) to this host. A null bitmap
    /// reverts to the host's default cursor. The bitmap ownership was transferred from the compositor; we own
    /// the resulting <see cref="Avalonia.Input.Cursor"/> and bitmap and dispose them on replace / Stop.
    /// </summary>
    internal void SetEmbeddedCursor(Bitmap? cursor, int hotspotX, int hotspotY)
    {
        _embeddedCursor?.Dispose();
        _embeddedCursorBitmap?.Dispose();
        if (cursor is null)
        {
            _embeddedCursor = null;
            _embeddedCursorBitmap = null;
            Cursor = null; // revert to the default cursor (we don't synthesize a hidden cursor here)
            return;
        }
        _embeddedCursorBitmap = cursor;
        _embeddedCursor = new Avalonia.Input.Cursor(cursor, new PixelPoint(hotspotX, hotspotY));
        Cursor = _embeddedCursor;
    }

    /// <summary>
    /// Scenario 5: set (or clear) the Avalonia content layer overlaid on this host. The overlay is a real
    /// Avalonia control added to this host's visual+logical tree and drawn on top of the toolkit bitmaps; its
    /// placement comes from <see cref="SetContentOverlayRect"/>. Driven by WaylandSubcompositorAvaloniaContentHost.
    /// </summary>
    internal void SetContentOverlay(Control? overlay)
    {
        if (ReferenceEquals(_contentOverlay, overlay))
            return;
        if (_contentOverlay is not null)
        {
            ((ISetLogicalParent)_contentOverlay).SetParent(null);
            LogicalChildren.Remove(_contentOverlay);
            VisualChildren.Remove(_contentOverlay);
        }
        _contentOverlay = overlay;
        if (overlay is not null)
        {
            ((ISetLogicalParent)overlay).SetParent(this);
            VisualChildren.Add(overlay);
            LogicalChildren.Add(overlay);
        }
        InvalidateMeasure();
    }

    /// <summary>Scenario 5: position/clip the content overlay within this host (host coordinate space). A null
    /// clip clips to the overlay's own bounds; a non-null clip is the visible region the glue reports.</summary>
    internal void SetContentOverlayRect(Rect rect, Rect? clip)
    {
        _contentRect = rect;
        _contentClip = clip;
        InvalidateMeasure();
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Embedding deliberately does NOT drive Avalonia layout from the toolkit's buffer size: the two layout
        // systems are fundamentally incompatible, and layout integration is the glue's job (scenario 1) or the
        // auto-window's (scenario 2). The control imposes no content-based desired size — it fills whatever it
        // is allocated and renders the surface tree stretched to fit (see Render). EmbeddedMinSize/MaxSize are
        // surfaced for the glue / auto-window to act on; they are intentionally not applied to layout here.
        _contentOverlay?.Measure(_contentRect.Size); // scenario 5: the overlay sizes to its glue-driven rect
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        // Resize proxy: when our allocated size changes, ask the embedded client to re-lay-out to fit. This is
        // the host-drives-size direction (the reverse of scenario 2's content-driven auto-window sizing).
        if (HostId != 0 && IsEmbeddedSurfaceMapped)
        {
            var w = (int)Math.Round(finalSize.Width);
            var h = (int)Math.Round(finalSize.Height);
            if (w > 0 && h > 0 && (w != _lastConfiguredWidth || h != _lastConfiguredHeight))
            {
                _lastConfiguredWidth = w;
                _lastConfiguredHeight = h;
                WaylandEmbeddingSubcompositor.Api.ConfigureToplevel(HostId, w, h);
                // Synchronously pull the client's re-rendered (new-size) frame before this Avalonia frame draws, so
                // the combined window never composites a stale buffer. Coalesced: many hosts / arrange passes in one
                // resize fold into a single client-loop tick. (No-op when no client-frame pump is registered.)
                WaylandEmbeddingSubcompositor.RequestResizeFlush(this);
            }
        }

        if (_contentOverlay is not null)
        {
            // Scenario 5: place the Avalonia content at the glue's rect. Clip to the reported visible region
            // (host coords → overlay-local); a null clip just clips to the overlay's own bounds.
            _contentOverlay.Arrange(_contentRect);
            _contentOverlay.ClipToBounds = true;
            _contentOverlay.Clip = _contentClip is { } clip
                ? new RectangleGeometry(new Rect(clip.X - _contentRect.X, clip.Y - _contentRect.Y, clip.Width, clip.Height))
                : null;
        }
        return result;
    }

    // ── pointer input forwarding (P2): map host-control DIPs to surface-local DIPs and post to the client.
    // Wayland requires a delivered enter before any motion/button/axis, and a leave only after an enter, so
    // we track _pointerEntered and never emit an unpaired event (an enter is suppressed until the surface has
    // committed — _clipWidth>0 — and is then sent lazily from the first event that can be mapped). ──
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        EnsurePointerEntered(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (EnsurePointerEntered(e) && TryMapToSurface(e, out var sx, out var sy))
            WaylandEmbeddingSubcompositor.Api.DeliverPointer(new PointerInputArgs(HostId, PointerEventKind.Motion, sx, sy));
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!_pointerEntered)
            return;
        _pointerEntered = false;
        WaylandEmbeddingSubcompositor.Api.DeliverPointer(new PointerInputArgs(HostId, PointerEventKind.Leave));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (EnsurePointerEntered(e)) // a press implies the pointer is over us
            ForwardButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind, pressed: true);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointerEntered)
            ForwardButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind, pressed: false);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!EnsurePointerEntered(e))
            return;
        // Avalonia delta: +Y up, +X right (notches); Wayland axis: +value down/right. ~10 units per notch.
        if (e.Delta.Y != 0)
            WaylandEmbeddingSubcompositor.Api.DeliverPointer(new PointerInputArgs(HostId, PointerEventKind.Axis, axis: 0, axisValue: -e.Delta.Y * 10));
        if (e.Delta.X != 0)
            WaylandEmbeddingSubcompositor.Api.DeliverPointer(new PointerInputArgs(HostId, PointerEventKind.Axis, axis: 1, axisValue: -e.Delta.X * 10));
    }

    /// <summary>Ensure the client has a live pointer enter on our surface; returns false if it can't be mapped yet.</summary>
    private bool EnsurePointerEntered(PointerEventArgs e)
    {
        if (_pointerEntered)
            return true;
        if (!TryMapToSurface(e, out var sx, out var sy))
            return false;
        _pointerEntered = true;
        WaylandEmbeddingSubcompositor.Api.DeliverPointer(new PointerInputArgs(HostId, PointerEventKind.Enter, sx, sy));
        return true;
    }

    private void ForwardButton(PointerUpdateKind kind, bool pressed)
    {
        var button = kind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => 0x110u,    // BTN_LEFT
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => 0x111u,   // BTN_RIGHT
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => 0x112u, // BTN_MIDDLE
            _ => 0u,
        };
        if (button != 0)
            WaylandEmbeddingSubcompositor.Api.DeliverPointer(new PointerInputArgs(HostId, PointerEventKind.Button, button: button, pressed: pressed));
    }

    private bool TryMapToSurface(PointerEventArgs e, out double surfaceX, out double surfaceY)
    {
        surfaceX = surfaceY = 0;
        if (HostId == 0 || _clipWidth <= 0 || _clipHeight <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            return false;
        // Un-stretch: control DIPs → surface-local DIPs (the surface's logical size is the clip).
        var pos = e.GetPosition(this);
        surfaceX = pos.X * _clipWidth / Bounds.Width;
        surfaceY = pos.Y * _clipHeight / Bounds.Height;
        return true;
    }

    // ── keyboard input forwarding (P2): focus → enter/leave, key events → wl_keyboard.key ──
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (HostId == 0)
            return;
        WaylandEmbeddingSubcompositor.Api.DeliverKeyboard(new KeyboardInputArgs(HostId, KeyboardEventKind.Enter));
        WaylandEmbeddingSubcompositor.Api.DeliverTextInput(new TextInputArgs(HostId, TextInputEventKind.Enter));
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (HostId == 0)
            return;
        WaylandEmbeddingSubcompositor.Api.DeliverKeyboard(new KeyboardInputArgs(HostId, KeyboardEventKind.Leave));
        WaylandEmbeddingSubcompositor.Api.DeliverTextInput(new TextInputArgs(HostId, TextInputEventKind.Leave));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        ForwardKey(e, pressed: true);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        ForwardKey(e, pressed: false);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (HostId != 0 && !string.IsNullOrEmpty(e.Text))
            WaylandEmbeddingSubcompositor.Api.DeliverTextInput(new TextInputArgs(HostId, TextInputEventKind.Commit, e.Text));
    }

    /// <summary>Avalonia IME → toolkit: forward composition preview as zwp_text_input_v3.preedit_string (an empty
    /// string clears it). Final committed text still flows via <see cref="OnTextInput"/> → commit_string.</summary>
    internal void ForwardPreedit(string? preeditText, int? cursorPos)
    {
        if (HostId == 0)
            return;
        var text = preeditText ?? "";
        var cursor = Utf8ByteOffset(text, cursorPos);
        WaylandEmbeddingSubcompositor.Api.DeliverTextInput(new TextInputArgs(HostId, TextInputEventKind.Preedit, text, cursor, cursor));
    }

    /// <summary>Toolkit → Avalonia IME: map the embedded caret rect (surface coords) into host coords and surface
    /// it to the IME bridge, so the OS candidate window positions over the embedded caret.</summary>
    internal void SetEmbeddedImeCursorRect(int x, int y, int width, int height)
    {
        var scaleX = _clipWidth > 0 && Bounds.Width > 0 ? Bounds.Width / _clipWidth : 1.0;
        var scaleY = _clipHeight > 0 && Bounds.Height > 0 ? Bounds.Height / _clipHeight : 1.0;
        _imClient.UpdateCursorRectangle(new Rect(x * scaleX, y * scaleY, width * scaleX, height * scaleY));
    }

    private static int Utf8ByteOffset(string text, int? cursorPos)
    {
        if (cursorPos is not { } pos)
            return System.Text.Encoding.UTF8.GetByteCount(text); // caret at the end of the preedit
        pos = Math.Clamp(pos, 0, text.Length);
        return System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(0, pos));
    }

    private void ForwardKey(KeyEventArgs e, bool pressed)
    {
        if (HostId == 0)
            return;
        var code = KeyCodes.ToEvdev(e.PhysicalKey);
        if (code == 0)
            return;
        // A key "produces text" (and so is delivered via the IME's commit_string rather than as a raw key when
        // a text-input is active) only if it yields a PRINTABLE character and isn't a command chord:
        //  - Ctrl/Alt/Meta chords (Ctrl+A, Alt+F) carry a KeySymbol but are shortcuts → forward raw.
        //  - control-char keys (Enter "\r", Tab "\t", Backspace "\b", Escape "") carry a control-char
        //    KeySymbol but must reach the client as raw keys (KWin sends Enter/Space raw even mid-IME; else
        //    Backspace/Enter stop working). Shift is fine — uppercase is normal text.
        var commandChord = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0;
        var producesText = !commandChord && !string.IsNullOrEmpty(e.KeySymbol) && !char.IsControl(e.KeySymbol![0]);
        WaylandEmbeddingSubcompositor.Api.DeliverKeyboard(
            new KeyboardInputArgs(HostId, KeyboardEventKind.Key, code, pressed, XkbModifiers(e.KeyModifiers), producesText));
    }

    private static uint XkbModifiers(KeyModifiers modifiers)
    {
        uint mask = 0;
        if (modifiers.HasFlag(KeyModifiers.Shift)) mask |= 1;   // ShiftMask
        if (modifiers.HasFlag(KeyModifiers.Control)) mask |= 4; // ControlMask
        if (modifiers.HasFlag(KeyModifiers.Alt)) mask |= 8;     // Mod1Mask
        if (modifiers.HasFlag(KeyModifiers.Meta)) mask |= 64;   // Mod4Mask
        return mask;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Native content size = the toplevel's logical geometry (clip), else the union of bitmap bounds.
        var contentWidth = _clipWidth;
        var contentHeight = _clipHeight;
        if (contentWidth <= 0 || contentHeight <= 0)
        {
            foreach (var item in _drawOrder)
                if (_bitmaps.TryGetValue(item.SurfaceId, out var b))
                {
                    contentWidth = Math.Max(contentWidth, item.X + b.Size.Width);
                    contentHeight = Math.Max(contentHeight, item.Y + b.Size.Height);
                }
        }

        if (contentWidth > 0 && contentHeight > 0 && Bounds.Width > 0 && Bounds.Height > 0 && _drawOrder.Count > 0)
        {
            // StretchContent: scale the native-size surface tree to fill our allocation (the buffer size is not a
            // layout input), so a stale buffer is shown scaled during a resize round-trip. Otherwise draw the
            // surface tree 1:1 at the top-left, clipped to our allocation — a not-yet-resized frame then leaves a
            // one-frame edge gap rather than scale-distorting (frame-perfect wrappers, paired with the resize flush).
            using (context.PushClip(new Rect(0, 0, Bounds.Width, Bounds.Height)))
            using (StretchContent
                ? context.PushTransform(Matrix.CreateScale(Bounds.Width / contentWidth, Bounds.Height / contentHeight))
                : default)
            {
                foreach (var item in _drawOrder)
                    if (_bitmaps.TryGetValue(item.SurfaceId, out var bmp))
                        context.DrawImage(bmp, new Rect(item.X, item.Y, bmp.Size.Width, bmp.Size.Height));
            }
        }

        ReleaseDeferredFrames();
    }

    /// <summary>
    /// Fire the frame callbacks deferred for the surfaces we just rendered (the present-pace throttle: the client
    /// is only allowed to paint its NEXT frame once this one has been shown). Called at the end of <see cref="Render"/>
    /// and, ahead of a synchronous resize flush, to thaw the client's frame clock so it can paint the new size now.
    /// </summary>
    internal void ReleaseDeferredFrames()
    {
        if (_pendingFrameIds.Length == 0)
            return;
        var ids = _pendingFrameIds;
        _pendingFrameIds = Array.Empty<uint>();
        WaylandEmbeddingSubcompositor.Api.FireFrameCallbacks(ids);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scaleHost = TopLevel.GetTopLevel(this);
        if (_scaleHost is not null)
            _scaleHost.ScalingChanged += OnHostScalingChanged;
        if (_scaleHost is Window activationWindow)
            activationWindow.PropertyChanged += OnHostWindowPropertyChanged;
        ReportPreferredScale();
        ReportActivation(force: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_scaleHost is Window activationWindow)
            activationWindow.PropertyChanged -= OnHostWindowPropertyChanged;
        if (_scaleHost is not null)
        {
            _scaleHost.ScalingChanged -= OnHostScalingChanged;
            _scaleHost = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnHostScalingChanged(object? sender, EventArgs e) => ReportPreferredScale();

    // Observe Window.IsActive via the property change (it fires AFTER the value updates — unlike the Activated
    // event, which is raised before IsActive is set), so the activation we read is never stale.
    private void OnHostWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowBase.IsActiveProperty)
            ReportActivation();
    }

    /// <summary>
    /// Report the embedded toplevel's <c>activated</c> state from the host's containing window. Toolkits gray out
    /// their whole UI when not activated, so activation must follow the WINDOW — not per-widget keyboard focus.
    /// When the host's <see cref="TopLevel"/> is not a <see cref="Window"/> (a popup root, or not yet attached),
    /// the content is always considered active so it is never grayed out. A host attached to a <see cref="Window"/>
    /// that is never shown reports inactive (the window genuinely isn't active) — that's the intended contract; the
    /// compositor's <c>activated</c> defaults to false, so a redundant inactive report sends no configure.
    /// (<see cref="_scaleHost"/> doubles as the activation window and must not be reassigned while attached — the
    /// attach/detach pair below is what keeps the subscription balanced.)
    /// </summary>
    private void ReportActivation(bool force = false)
    {
        if (HostId == 0)
            return;
        var active = _scaleHost is not Window window || window.IsActive;
        if (!force && _activeReported && active == _lastActive)
            return;
        _activeReported = true;
        _lastActive = active;
        WaylandEmbeddingSubcompositor.Api.SetActivated(HostId, active);
    }

    /// <summary>Tell the compositor our render scaling so it can hint the client via preferred_buffer_scale.</summary>
    private void ReportPreferredScale()
    {
        if (HostId == 0)
            return;
        var renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var scale = Math.Max(1, (int)Math.Ceiling(renderScaling));
        if (scale == _reportedScale)
            return;
        _reportedScale = scale;
        WaylandEmbeddingSubcompositor.Api.SetHostScale(HostId, scale);
    }

    /// <summary>
    /// Merge incoming frame-surface ids into the pending set, de-duplicating: a surface id already queued for
    /// the next render's frame-callback fire need not be posted twice (firing is idempotent, but duplicates
    /// are wasted work). Order-preserving. Internal for direct unit testing.
    /// </summary>
    internal static uint[] AppendFrameIds(uint[] existing, IReadOnlyList<uint> incoming)
    {
        if (incoming.Count == 0)
            return existing;

        var seen = new HashSet<uint>(existing.Length + incoming.Count);
        var result = new List<uint>(existing.Length + incoming.Count);
        foreach (var id in existing)
            if (seen.Add(id))
                result.Add(id);
        for (var i = 0; i < incoming.Count; i++)
            if (seen.Add(incoming[i]))
                result.Add(incoming[i]);
        return result.ToArray();
    }
}
