using System;
using System.Collections.Generic;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Wayland.Server.Persistent;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient;

/// <summary>
/// Worker-side facade for <c>zwp_text_input_v3</c>. Owns the per-seat protocol
/// proxies and per-surface state cache. Bridges incoming compositor events into
/// the broker via <see cref="IWaylandTextInputV3Events"/>.
/// </summary>
/// <remarks>
/// All public methods must be called on the Wayland worker thread (post via
/// <c>WaylandWorkerClient.PostOob</c> from the UI thread).
/// </remarks>
internal sealed class WaylandTextInputV3(ZwpTextInputManagerV3 manager)
{
    private readonly Dictionary<uint, PerSeat> _seats = new();
    private readonly Dictionary<WSurface, PerSurface> _surfaces = new();

    public void OnSeatAdded(uint seatGlobalName, WlSeat wlSeat)
    {
        if (_seats.ContainsKey(seatGlobalName))
            return;
        var seat = new PerSeat(this, seatGlobalName);
        seat.Proxy = manager.GetTextInput(wlSeat, seat.CreateListener(), null);
        _seats[seatGlobalName] = seat;
    }

    public void OnSeatRemoved(uint seatGlobalName)
    {
        if (!_seats.Remove(seatGlobalName, out var seat))
            return;
        if (seat.FocusedSurface is { } surface
            && _surfaces.TryGetValue(surface, out var perSurface))
        {
            perSurface.EnteredSeats.Remove(seat);
        }
        try { seat.Proxy?.Destroy(); } catch { }
        seat.Proxy?.Dispose();
    }

    public void Dispose()
    {
        foreach (var seat in _seats.Values)
        {
            try { seat.Proxy?.Destroy(); } catch { }
            seat.Proxy?.Dispose();
        }
        _seats.Clear();
        _surfaces.Clear();
        try { manager.Destroy(); } catch { }
        manager.Dispose();
    }

    public void RegisterSurface(WSurface s, WaylandTextInputV3EventsProxy sink)
    {
        if (_surfaces.ContainsKey(s))
            return;
        var ps = new PerSurface(s, sink);
        // Reconcile: if any seat's text-input-v3 already has focus on this
        // surface (enter event arrived before the broker registered), record
        // it now so a subsequent SetActive(true) can issue Enable+Commit.
        foreach (var seat in _seats.Values)
        {
            if (ReferenceEquals(seat.FocusedSurface, s))
                ps.EnteredSeats.Add(seat);
        }
        _surfaces[s] = ps;
    }

    public void UnregisterSurface(WSurface s)
    {
        if (!_surfaces.Remove(s, out var perSurface))
            return;
        // For each seat currently entered on this surface, send disable+commit
        // so the compositor stops routing IME events to a now-dead surface.
        foreach (var seat in perSurface.EnteredSeats)
        {
            if (ReferenceEquals(seat.FocusedSurface, s))
                seat.FocusedSurface = null;
            DisableAndClear(seat, perSurface);
        }
    }

    public void SetActive(WSurface s, bool hasClient, bool supportsPreedit, bool supportsSurroundingText)
    {
        if (!_surfaces.TryGetValue(s, out var ps))
            return;
        ps.HasClient = hasClient;
        ps.SupportsPreedit = supportsPreedit;
        ps.SupportsSurroundingText = supportsSurroundingText;
        PushStateToEnteredSeats(s, ps);
    }

    /// <summary>
    /// Aborts any in-flight IME composition for this surface: sends
    /// disable+commit on every entered seat and clears the per-seat
    /// last-delivered state so the next preedit is not deduped away.
    /// Also clears cached surrounding text so the next client starts clean.
    /// Called when the broker's TextInputMethodClient changes.
    /// </summary>
    public void AbortComposition(WSurface s)
    {
        if (!_surfaces.TryGetValue(s, out var ps))
            return;
        foreach (var seat in ps.EnteredSeats)
        {
            DisableAndClear(seat, ps);
            seat.ClearLastDelivered();
        }
        ps.LastSentEnabled = false;
        ps.SurroundingText = null;
        ps.SurroundingCursorChar = 0;
        ps.SurroundingAnchorChar = 0;
        ps.CursorRect = null;
        ps.Options = null;
        ps.HasClient = false;
    }

    public void SetCursorRect(WSurface s, Rect logical)
    {
        if (!_surfaces.TryGetValue(s, out var ps))
            return;
        ps.CursorRect = logical;
        if (!ps.LastSentEnabled)
            return;
        var pixelRect = TranslateCursorRect(s, logical);
        foreach (var seat in ps.EnteredSeats)
        {
            if (seat.Proxy is not { } proxy)
                continue;
            proxy.SetCursorRectangle(pixelRect.x, pixelRect.y, pixelRect.w, pixelRect.h);
            seat.Commit();
        }
    }

    public void SetOptions(WSurface s, TextInputOptions options)
    {
        if (!_surfaces.TryGetValue(s, out var ps))
            return;
        ps.Options = options;
        if (!ps.LastSentEnabled)
            return;
        var (hint, purpose) = TextInputOptionsConverter.Convert(options);
        foreach (var seat in ps.EnteredSeats)
        {
            if (seat.Proxy is not { } proxy)
                continue;
            proxy.SetContentType(hint, purpose);
            seat.Commit();
        }
    }

    public void SetSurroundingText(WSurface s, string text, int cursorChar, int anchorChar)
    {
        if (!_surfaces.TryGetValue(s, out var ps))
            return;
        ps.SurroundingText = text;
        ps.SurroundingCursorChar = cursorChar;
        ps.SurroundingAnchorChar = anchorChar;
        if (!ps.LastSentEnabled || !ps.SupportsSurroundingText)
            return;

        // Compositors enforce a 4000-byte cap per spec; truncate around cursor.
        var (clampedText, cursorByte, anchorByte) = ClampSurroundingForWayland(text, cursorChar, anchorChar);

        foreach (var seat in ps.EnteredSeats)
        {
            if (seat.Proxy is not { } proxy)
                continue;
            proxy.SetSurroundingText(clampedText, cursorByte, anchorByte);
            proxy.SetTextChangeCause(ZwpTextInputV3.ChangeCauseEnum.Other);
            seat.Commit();
        }
    }

    public void Reset(WSurface s)
    {
        // v3 has no reset request. Closest safe behaviour: drop pending preedit
        // by issuing disable + enable + commit. Compositor treats this as a fresh
        // session and discards in-flight preedit.
        if (!_surfaces.TryGetValue(s, out var ps) || !ps.HasClient)
            return;
        var hadDeliveredPreedit = false;
        foreach (var seat in ps.EnteredSeats)
        {
            if (seat.Proxy is not { } proxy)
                continue;
            if (!string.IsNullOrEmpty(seat.LastDeliveredPreedit))
                hadDeliveredPreedit = true;
            seat.ClearPending();
            seat.ClearLastDelivered();
            proxy.Disable();
            seat.Commit();
            proxy.Enable();
            PushSeatStateLocked(seat, s, ps);
            seat.Commit();
        }
        // Clear stale composition on the client side too — otherwise the visible
        // preedit lingers until the IME (re-)sends one, which may be deduped.
        if (hadDeliveredPreedit)
            ps.Sink.OnImeUpdate(ps.Surface.TextInputSessionToken, 0, 0, null, null, -1, -1);
    }

    private static void DisableAndClear(PerSeat seat, PerSurface ps)
    {
        if (ps.LastSentEnabled && seat.Proxy is { } proxy)
        {
            try
            {
                proxy.Disable();
                seat.Commit();
            }
            catch { }
        }
        seat.ClearPending();
    }

    private static void PushStateToEnteredSeats(WSurface s, PerSurface ps)
    {
        var enable = ps.HasClient;
        foreach (var seat in ps.EnteredSeats)
        {
            if (seat.Proxy is not { } proxy)
                continue;
            if (enable)
            {
                proxy.Enable();
                PushSeatStateLocked(seat, s, ps);
            }
            else if (ps.LastSentEnabled)
            {
                proxy.Disable();
            }
            seat.Commit();
        }
        ps.LastSentEnabled = enable;
    }

    private static void PushSeatStateLocked(PerSeat seat, WSurface s, PerSurface ps)
    {
        if (seat.Proxy is not { } proxy)
            return;
        if (ps.Options is { } options)
        {
            var (hint, purpose) = TextInputOptionsConverter.Convert(options);
            proxy.SetContentType(hint, purpose);
        }
        if (ps.CursorRect is { } rect)
        {
            var (x, y, w, h) = TranslateCursorRect(s, rect);
            proxy.SetCursorRectangle(x, y, w, h);
        }
        if (ps.SupportsSurroundingText && ps.SurroundingText is { } text)
        {
            var (clampedText, cursorByte, anchorByte) =
                ClampSurroundingForWayland(text, ps.SurroundingCursorChar, ps.SurroundingAnchorChar);
            proxy.SetSurroundingText(clampedText, cursorByte, anchorByte);
            proxy.SetTextChangeCause(ZwpTextInputV3.ChangeCauseEnum.Other);
        }
    }

    private static (int x, int y, int w, int h) TranslateCursorRect(WSurface s, Rect logical)
    {
        // The cursor rectangle is in surface-local logical units. Under fractional
        // scaling this matches wp_viewport.set_destination units (also surface-local
        // logical). Add shadow extents so the candidate window appears next to the
        // editable content area, not next to the drop shadow.
        var shadow = (s as WXdgShellSurface)?.ShadowExtents ?? default;
        var x = (int)Math.Round(logical.X) + (int)Math.Round(shadow.Left);
        var y = (int)Math.Round(logical.Y) + (int)Math.Round(shadow.Top);
        var w = Math.Max(1, (int)Math.Round(logical.Width));
        var h = Math.Max(1, (int)Math.Round(logical.Height));
        return (x, y, w, h);
    }

    private static (string text, int cursorByte, int anchorByte) ClampSurroundingForWayland(
        string text, int cursorChar, int anchorChar)
    {
        const int MaxBytes = 4000;
        var totalBytes = System.Text.Encoding.UTF8.GetByteCount(text);
        if (totalBytes <= MaxBytes)
        {
            return (
                text,
                WaylandTextUtils.Utf8ByteIndexFromCharIndex(text, cursorChar),
                WaylandTextUtils.Utf8ByteIndexFromCharIndex(text, anchorChar));
        }

        // Trim around cursor: keep ~half of MaxBytes on each side. The protocol
        // accepts text that doesn't extend to start/end as a partial window.
        var half = MaxBytes / 2;
        var cursorClamped = Math.Clamp(cursorChar, 0, text.Length);
        var startChar = WaylandTextUtils.CharIndexFromUtf8Offset(text, -half, cursorClamped);
        var endChar = WaylandTextUtils.CharIndexFromUtf8Offset(text, half, cursorClamped);
        var trimmed = text.Substring(startChar, endChar - startChar);

        var cursorByte = WaylandTextUtils.Utf8ByteIndexFromCharIndex(trimmed,
            Math.Clamp(cursorChar - startChar, 0, trimmed.Length));
        var anchorByte = WaylandTextUtils.Utf8ByteIndexFromCharIndex(trimmed,
            Math.Clamp(anchorChar - startChar, 0, trimmed.Length));
        return (trimmed, cursorByte, anchorByte);
    }

    // ----- inner state types -----

    private sealed class PerSurface(WSurface surface, WaylandTextInputV3EventsProxy sink)
    {
        public WSurface Surface { get; } = surface;
        public WaylandTextInputV3EventsProxy Sink { get; } = sink;
        public List<PerSeat> EnteredSeats { get; } = new();
        public bool HasClient;
        public bool SupportsPreedit;
        public bool SupportsSurroundingText;
        public bool LastSentEnabled;
        public Rect? CursorRect;
        public TextInputOptions? Options;
        public string? SurroundingText;
        public int SurroundingCursorChar;
        public int SurroundingAnchorChar;
    }

    private sealed class PerSeat(WaylandTextInputV3 owner, uint globalName)
    {
        public uint GlobalName { get; } = globalName;
        public ZwpTextInputV3? Proxy;
        public WSurface? FocusedSurface;
        public uint CommitSerial;
        public uint LastDoneSerial;

        // Pending state, accumulated until a done event. The protocol uses
        // a double-buffered model: preedit_string / commit_string /
        // delete_surrounding_text events stage values that are applied — and
        // reset — atomically on each done. So a done with no preceding
        // preedit_string event clears the preedit; this is how the protocol
        // model works, not a special-case rule.
        public string PendingPreedit = string.Empty;
        public int PendingPreeditCursorBeginByte;
        public int PendingPreeditCursorEndByte = -1;
        public string PendingCommit = string.Empty;
        public bool HasDelete;
        public uint PendingDeleteBeforeBytes;
        public uint PendingDeleteAfterBytes;

        // Last delivered preedit (for done-event dedup).
        public string? LastDeliveredPreedit;
        public int LastDeliveredCursorBeginChar;
        public int LastDeliveredCursorEndChar;

        public ZwpTextInputV3.Listener CreateListener() => new SeatListener(owner, this);

        public void Commit()
        {
            CommitSerial++;
            Proxy?.Commit();
        }

        public void ClearPending()
        {
            PendingPreedit = string.Empty;
            PendingPreeditCursorBeginByte = 0;
            PendingPreeditCursorEndByte = -1;
            PendingCommit = string.Empty;
            HasDelete = false;
            PendingDeleteBeforeBytes = PendingDeleteAfterBytes = 0;
        }

        public void ClearLastDelivered()
        {
            LastDeliveredPreedit = null;
            LastDeliveredCursorBeginChar = 0;
            LastDeliveredCursorEndChar = 0;
        }
    }

    private sealed class SeatListener(WaylandTextInputV3 owner, PerSeat seat) : ZwpTextInputV3.Listener
    {
        protected override void Enter(ZwpTextInputV3 sender, WlSurface? surface)
        {
            var ws = ResolveSurface(surface);
            if (ws == null)
                return;
            seat.FocusedSurface = ws;
            if (!owner._surfaces.TryGetValue(ws, out var ps))
                return;
            ps.EnteredSeats.Remove(seat);
            ps.EnteredSeats.Add(seat);
            seat.ClearPending();

            if (ps.HasClient && seat.Proxy is { } proxy)
            {
                proxy.Enable();
                PushSeatStateLocked(seat, ws, ps);
                seat.Commit();
                ps.LastSentEnabled = true;
            }
        }

        protected override void Leave(ZwpTextInputV3 sender, WlSurface? surface)
        {
            var ws = ResolveSurface(surface);
            if (ws == null && seat.FocusedSurface == null)
                return;
            ws ??= seat.FocusedSurface;
            seat.FocusedSurface = null;
            seat.ClearPending();
            if (ws != null && owner._surfaces.TryGetValue(ws, out var ps))
            {
                ps.EnteredSeats.Remove(seat);
                if (ps.EnteredSeats.Count == 0)
                    ps.LastSentEnabled = false;

                // Per spec, leaving invalidates any in-flight preedit. If we
                // had delivered preedit text from this seat, clear it on the
                // client side so the user doesn't see stale composition text.
                if (!string.IsNullOrEmpty(seat.LastDeliveredPreedit))
                {
                    ps.Sink.OnImeUpdate(ps.Surface.TextInputSessionToken, 0, 0, null, null, -1, -1);
                }
            }
            seat.ClearLastDelivered();
        }

        protected override void PreeditString(ZwpTextInputV3 sender, string? text, int cursorBegin, int cursorEnd)
        {
            seat.PendingPreedit = text ?? string.Empty;
            seat.PendingPreeditCursorBeginByte = cursorBegin;
            seat.PendingPreeditCursorEndByte = cursorEnd;
        }

        protected override void CommitString(ZwpTextInputV3 sender, string? text)
        {
            seat.PendingCommit = text ?? string.Empty;
        }

        protected override void DeleteSurroundingText(ZwpTextInputV3 sender, uint beforeLength, uint afterLength)
        {
            seat.HasDelete = true;
            seat.PendingDeleteBeforeBytes = beforeLength;
            seat.PendingDeleteAfterBytes = afterLength;
        }

        protected override void Done(ZwpTextInputV3 sender, uint serial)
        {
            seat.LastDoneSerial = serial;

            // Snapshot pending state, then clear immediately. The protocol's
            // double-buffered model requires pending preedit/commit/delete
            // to be consumed atomically on each done — so a subsequent done
            // with no preedit_string event clears the preedit.
            var pendingPreedit = seat.PendingPreedit;
            var pendingPreeditBeginByte = seat.PendingPreeditCursorBeginByte;
            var pendingPreeditEndByte = seat.PendingPreeditCursorEndByte;
            var pendingCommit = seat.PendingCommit;
            var hasDelete = seat.HasDelete;
            var pendingDeleteBefore = seat.PendingDeleteBeforeBytes;
            var pendingDeleteAfter = seat.PendingDeleteAfterBytes;
            seat.ClearPending();

            // Only the active seat for its focused surface surfaces IME state.
            var surface = seat.FocusedSurface;
            if (surface == null
                || !owner._surfaces.TryGetValue(surface, out var ps)
                || ps.EnteredSeats.Count == 0
                || ps.EnteredSeats[ps.EnteredSeats.Count - 1] != seat)
            {
                if (Logger.TryGet(LogEventLevel.Verbose, "Wayland.TextInput") is { } log)
                    log.Log(this, "Dropping IME 'done' from non-active seat {Seat}", seat.GlobalName);
                return;
            }

            var sink = ps.Sink;
            // Per spec (zwp_text_input_v3.done): "The serial number reflects
            // the last state of zwp_text_input_v3.commit request. The client
            // is expected to compare this serial with the most recent commit's
            // serial it has sent. If they don't match the client must ignore
            // the changes." Every enable/disable/state push we make ends in a
            // commit() that bumps commit_count, so a stale serial means the
            // batch was generated against state we've already superseded
            // (e.g. a prior focus/client session). Drop the whole batch.
            if (serial != seat.CommitSerial)
            {
                if (Logger.TryGet(LogEventLevel.Verbose, "Wayland.TextInput") is { } staleLog)
                    staleLog.Log(this,
                        "Dropping stale text-input-v3 done batch (done.serial={Serial}, commit_count={Commit})",
                        serial, seat.CommitSerial);
                return;
            }

            // Spec event order on the client side:
            //   1. clear current preedit (implicit before commit/delete);
            //   2. apply delete_surrounding_text;
            //   3. insert commit text;
            //   4. apply new preedit.
            // We deliver all four operations to the UI thread as a SINGLE
            // proxy call so they apply atomically (no other UI work can
            // interleave between them).
            var hadPreedit = !string.IsNullOrEmpty(seat.LastDeliveredPreedit);
            var commitText = pendingCommit.Length > 0 ? pendingCommit : null;
            var hasCommit = commitText != null;

            int deleteBeforeChars = 0, deleteAfterChars = 0;
            if (hasDelete && ps.SupportsSurroundingText)
                (deleteBeforeChars, deleteAfterChars) = ConvertDeleteToChars(
                    ps, pendingDeleteBefore, pendingDeleteAfter);

            // Compute new preedit (null = clear).
            int beginChar, endChar;
            if (pendingPreeditBeginByte < 0 || pendingPreeditEndByte < 0)
            {
                beginChar = endChar = -1;
            }
            else
            {
                beginChar = WaylandTextUtils.CharIndexFromUtf8Offset(pendingPreedit, pendingPreeditBeginByte, 0);
                endChar = WaylandTextUtils.CharIndexFromUtf8Offset(pendingPreedit, pendingPreeditEndByte, 0);
            }
            var newPreedit = pendingPreedit.Length == 0 ? null : pendingPreedit;

            var preeditUnchanged = seat.LastDeliveredPreedit == newPreedit
                && seat.LastDeliveredCursorBeginChar == beginChar
                && seat.LastDeliveredCursorEndChar == endChar;

            // Skip the entire batch only when nothing observable happens: no
            // commit, no delete, and the preedit is byte-for-byte identical
            // to what we last sent.
            var hasObservableDelete = deleteBeforeChars > 0 || deleteAfterChars > 0;
            if (!hasCommit && !hasObservableDelete && preeditUnchanged)
                return;

            sink.OnImeUpdate(
                ps.Surface.TextInputSessionToken,
                deleteBeforeChars, deleteAfterChars,
                commitText,
                newPreedit, beginChar, endChar);

            seat.LastDeliveredPreedit = newPreedit;
            seat.LastDeliveredCursorBeginChar = beginChar;
            seat.LastDeliveredCursorEndChar = endChar;
        }

        private static (int beforeChars, int afterChars) ConvertDeleteToChars(PerSurface ps, uint beforeBytes, uint afterBytes)
        {
            // No surrounding-text snapshot means we can't safely convert the
            // protocol's UTF-8 byte counts to UTF-16 char counts (passing the
            // bytes through would be wrong for any non-ASCII text). The IME
            // shouldn't issue delete_surrounding_text without us first sending
            // set_surrounding_text, so just drop.
            if (ps.SurroundingText is null)
                return (0, 0);
            var cursorChar = ps.SurroundingCursorChar;
            var beforeStartChar = WaylandTextUtils.CharIndexFromUtf8Offset(ps.SurroundingText, -(int)beforeBytes, cursorChar);
            var afterEndChar = WaylandTextUtils.CharIndexFromUtf8Offset(ps.SurroundingText, (int)afterBytes, cursorChar);
            return (cursorChar - beforeStartChar, afterEndChar - cursorChar);
        }

        private static WSurface? ResolveSurface(WlSurface? wlSurface)
        {
            if (wlSurface == null)
                return null;
            if (wlSurface.Tags.TryGetValue(typeof(WXdgShellSurface), out var tag))
                return (WSurface)tag;
            return null;
        }
    }
}
