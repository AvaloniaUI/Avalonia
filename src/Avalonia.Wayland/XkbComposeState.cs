using System;
using System.Text;
using static Avalonia.Wayland.Server.Interop.XkbCommonNativeMethods;

namespace Avalonia.Wayland;

/// <summary>
/// Wraps an xkb_compose_state for tracking compose/dead key sequences.
/// </summary>
sealed unsafe class XkbComposeState : IDisposable
{
    private IntPtr _state;

    public XkbComposeState(XkbComposeTable table)
    {
        _state = xkb_compose_state_new(table.Handle, XKB_COMPOSE_STATE_NO_FLAGS);
        if (_state == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create xkb compose state");
    }

    /// <summary>
    /// Feeds a keysym to the compose state machine.
    /// Returns the compose status after feeding.
    /// </summary>
    public int Feed(uint keysym)
    {
        if (_state == IntPtr.Zero)
            return XKB_COMPOSE_NOTHING;

        xkb_compose_state_feed(_state, keysym);
        return xkb_compose_state_get_status(_state);
    }

    /// <summary>
    /// Gets the composed UTF-8 text. Only valid when status is XKB_COMPOSE_COMPOSED.
    /// </summary>
    public string? GetComposedText()
    {
        if (_state == IntPtr.Zero)
            return null;

        var needed = xkb_compose_state_get_utf8(_state, null, IntPtr.Zero);
        if (needed <= 0)
            return null;

        var bufSize = needed + 1;
        var buf = stackalloc byte[bufSize];
        xkb_compose_state_get_utf8(_state, buf, (IntPtr)bufSize);

        return Encoding.UTF8.GetString(buf, needed);
    }

    /// <summary>
    /// Gets the composed keysym. Only valid when status is XKB_COMPOSE_COMPOSED.
    /// </summary>
    public uint GetComposedKeysym()
    {
        if (_state == IntPtr.Zero)
            return 0;

        return xkb_compose_state_get_one_sym(_state);
    }

    public void Reset()
    {
        if (_state != IntPtr.Zero)
            xkb_compose_state_reset(_state);
    }

    public void Dispose()
    {
        if (_state != IntPtr.Zero)
        {
            xkb_compose_state_unref(_state);
            _state = IntPtr.Zero;
        }
    }
}
