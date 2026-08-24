using System;
using System.Text;
using Avalonia.Wayland.Server.Interop;
using static Avalonia.Wayland.Server.Interop.XkbCommonNativeMethods;

namespace Avalonia.Wayland;

/// <summary>
/// Wraps an xkbcommon keymap and state, providing safe .NET API for keyboard layout handling.
/// Created from the keymap fd received in the wl_keyboard.keymap event.
/// </summary>
sealed unsafe class XkbCommonKeymap : IDisposable
{
    private IntPtr _keymap;
    private IntPtr _state;

    public XkbCommonKeymap(XkbContext context, int fd, uint size)
    {
        var mapped = UnsafeNativeMethods.mmap(
            IntPtr.Zero, (IntPtr)size, PROT_READ, MAP_PRIVATE, fd, IntPtr.Zero);
        UnsafeNativeMethods.close(fd);

        if (mapped == IntPtr.Zero || mapped == new IntPtr(-1))
            throw new InvalidOperationException("Failed to mmap keymap fd");

        try
        {
            // size includes null terminator; xkb_keymap_new_from_buffer expects length without it
            _keymap = xkb_keymap_new_from_buffer(
                context.Handle, (byte*)mapped, (IntPtr)(size - 1),
                XKB_KEYMAP_FORMAT_TEXT_V1, XKB_KEYMAP_COMPILE_NO_FLAGS);

            if (_keymap == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create xkb keymap from buffer");

            _state = xkb_state_new(_keymap);
            if (_state == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create xkb state");
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            UnsafeNativeMethods.munmap(mapped, (IntPtr)size);
        }
    }

    /// <summary>
    /// Number of keyboard layout groups in this keymap.
    /// </summary>
    public uint LayoutCount => _keymap != IntPtr.Zero ? xkb_keymap_num_layouts(_keymap) : 0;

    /// <summary>
    /// Updates the keyboard state with new modifier and group values from wl_keyboard.modifiers.
    /// </summary>
    public void UpdateModifiers(uint modsDepressed, uint modsLatched, uint modsLocked, uint group)
    {
        if (_state != IntPtr.Zero)
            xkb_state_update_mask(_state, modsDepressed, modsLatched, modsLocked, 0, 0, group);
    }

    /// <summary>
    /// Resolves an evdev keycode to a keysym and UTF-8 text using the current xkb state.
    /// </summary>
    /// <param name="evdevKeycode">The evdev keycode (NOT xkb keycode — offset +8 is applied internally).</param>
    /// <returns>The keysym and the UTF-8 text produced by the key, or null if no text.</returns>
    public (uint keysym, string? text) ResolveKey(uint evdevKeycode)
    {
        if (_state == IntPtr.Zero)
            return (0, null);

        var xkbKey = evdevKeycode + 8;
        var keysym = xkb_state_key_get_one_sym(_state, xkbKey);

        // Get UTF-8 text: first call with null to get required size
        var needed = xkb_state_key_get_utf8(_state, xkbKey, null, IntPtr.Zero);
        if (needed <= 0)
            return (keysym, null);

        // Allocate buffer (+1 for null terminator written by xkbcommon)
        var bufSize = needed + 1;
        var buf = stackalloc byte[bufSize];
        xkb_state_key_get_utf8(_state, xkbKey, buf, (IntPtr)bufSize);

        var text = Encoding.UTF8.GetString(buf, needed);
        return (keysym, text);
    }

    /// <summary>
    /// Gets the keysym for a key in a specific layout group at level 0 (base level).
    /// Used for non-latin keyboard fallback — cycling through layouts to find a latin key.
    /// </summary>
    /// <param name="evdevKeycode">The evdev keycode.</param>
    /// <param name="layout">The layout group index.</param>
    /// <returns>The keysym, or 0 if not found.</returns>
    public uint FindKeysymInLayout(uint evdevKeycode, uint layout)
    {
        if (_keymap == IntPtr.Zero)
            return 0;

        var xkbKey = evdevKeycode + 8;
        var count = xkb_keymap_key_get_syms_by_level(_keymap, xkbKey, layout, 0, out var syms);
        if (count > 0 && syms != null)
            return syms[0];

        return 0;
    }

    public void Dispose()
    {
        if (_state != IntPtr.Zero)
        {
            xkb_state_unref(_state);
            _state = IntPtr.Zero;
        }

        if (_keymap != IntPtr.Zero)
        {
            xkb_keymap_unref(_keymap);
            _keymap = IntPtr.Zero;
        }
    }
}
