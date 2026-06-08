using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Wayland.Server.Interop;
using NWayland.Interop;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient;

unsafe class WaylandCursorManager : IDisposable
{
    private readonly IntPtr _theme;
    private readonly Dictionary<StandardCursorType, CursorEntry?> _cursors = new();
    private readonly WlDisplay _display;
    private readonly WlCompositor _compositor;

    internal readonly record struct CursorEntry(WlSurface Surface, WlBuffer buffer, int HotspotX, int HotspotY);

    private static readonly Dictionary<StandardCursorType, string[]> CursorNames = new()
    {
        { StandardCursorType.Arrow, ["default", "left_ptr"] },
        { StandardCursorType.Ibeam, ["text", "xterm"] },
        { StandardCursorType.Wait, ["wait", "watch"] },
        { StandardCursorType.Cross, ["crosshair", "cross"] },
        { StandardCursorType.UpArrow, ["up_arrow", "sb_up_arrow"] },
        { StandardCursorType.SizeWestEast, ["ew-resize", "col-resize", "sb_h_double_arrow"] },
        { StandardCursorType.SizeNorthSouth, ["ns-resize", "row-resize", "sb_v_double_arrow"] },
        { StandardCursorType.SizeAll, ["all-scroll", "fleur"] },
        { StandardCursorType.No, ["not-allowed", "crossed_circle"] },
        { StandardCursorType.Hand, ["pointer", "hand2", "pointing_hand"] },
        { StandardCursorType.AppStarting, ["progress", "left_ptr_watch"] },
        { StandardCursorType.Help, ["help", "question_arrow"] },
        { StandardCursorType.TopSide, ["top_side", "n-resize"] },
        { StandardCursorType.BottomSide, ["bottom_side", "s-resize"] },
        { StandardCursorType.LeftSide, ["left_side", "w-resize"] },
        { StandardCursorType.RightSide, ["right_side", "e-resize"] },
        { StandardCursorType.TopLeftCorner, ["top_left_corner", "nw-resize"] },
        { StandardCursorType.TopRightCorner, ["top_right_corner", "ne-resize"] },
        { StandardCursorType.BottomLeftCorner, ["bottom_left_corner", "sw-resize"] },
        { StandardCursorType.BottomRightCorner, ["bottom_right_corner", "se-resize"] },
        { StandardCursorType.DragMove, ["grabbing", "dnd-move"] },
        { StandardCursorType.DragCopy, ["copy", "dnd-copy"] },
        { StandardCursorType.DragLink, ["alias", "dnd-link"] },
    };

    public WaylandCursorManager(WlDisplay display, WlShm shm, WlCompositor compositor)
    {
        _display = display;
        _compositor = compositor;
        _theme = UnsafeNativeMethods.wl_cursor_theme_load(null, 24, shm.Handle);
        if (_theme == IntPtr.Zero)
            throw new AvaloniaWaylandException("Failed to load default cursor theme");

        foreach (var type in CursorNames.Keys)
            LoadCursor(type);
    }

    private void LoadCursor(StandardCursorType type)
    {
        if (!CursorNames.TryGetValue(type, out var names))
            return;

        UnsafeNativeMethods.wl_cursor* cursor = null;
        foreach (var name in names)
        {
            cursor = UnsafeNativeMethods.wl_cursor_theme_get_cursor(_theme, name);
            if (cursor != null)
                break;
        }

        if (cursor == null || cursor->image_count == 0)
        {
            _cursors[type] = null;
            return;
        }

        var image = cursor->images[0];
        var buffer = UnsafeNativeMethods.wl_cursor_image_get_buffer(image);
        if (buffer == IntPtr.Zero)
        {
            _cursors[type] = null;
            return;
        }

        var surface = _compositor.CreateSurface(null);
        var wlBuffer =  WlBuffer.Import(this._display, null, buffer, true, null);
        surface.Attach(wlBuffer, 0, 0);
        surface.Commit();

        _cursors[type] = new CursorEntry(surface, wlBuffer, (int)image->hotspot_x, (int)image->hotspot_y);
    }

    /// <summary>
    /// Gets the cursor surface and hotspot for the given cursor type.
    /// Returns null for <see cref="StandardCursorType.None"/> or unknown cursors (hides cursor).
    /// Falls back to Arrow if the specific cursor is not available.
    /// </summary>
    internal CursorEntry? GetCursor(StandardCursorType type)
    {
        if (type == StandardCursorType.None)
            return null;

        if (_cursors.TryGetValue(type, out var entry))
            return entry ?? (type != StandardCursorType.Arrow ? GetCursor(StandardCursorType.Arrow) : null);

        return GetCursor(StandardCursorType.Arrow);
    }

    public void Dispose()
    {
        foreach (var entry in _cursors.Values)
            entry?.Surface.Dispose();
        _cursors.Clear();

        if (_theme != IntPtr.Zero)
            UnsafeNativeMethods.wl_cursor_theme_destroy(_theme);
    }
}
