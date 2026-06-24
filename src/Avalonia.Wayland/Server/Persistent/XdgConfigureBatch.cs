using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Server.Persistent;

[Flags]
enum XdgToplevelStates
{
    None = 0,
    Maximized = 1 << 0,
    Fullscreen = 1 << 1,
    Resizing = 1 << 2,
    Activated = 1 << 3,
    TiledLeft = 1 << 4,
    TiledRight = 1 << 5,
    TiledTop = 1 << 6,
    TiledBottom = 1 << 7,
    Suspended = 1 << 8,
}

class XdgConfigureBatch
{
    public PixelSize? Bounds;
    public PixelSize Size;
    public XdgToplevelStates States;
    public uint Serial;

    /// <summary>
    /// Maximum usable surface size, computed from Bounds or output geometry.
    /// </summary>
    public Size MaxSize;

    /// <summary>
    /// Effective decoration mode after the initial handshake.
    /// <c>null</c> = no decoration object was created (compositor doesn't
    /// support zxdg_decoration_manager_v1, or sticky-CSD is on, or the
    /// <c>ForceDrawnDecorations</c> option suppressed binding) — UI side
    /// must draw client-side chrome.
    /// </summary>
    public DecorationMode? InitialDecorationMode;

    public static XdgToplevelStates ParseStates(ReadOnlySpan<byte> data)
    {
        var states = XdgToplevelStates.None;
        var uint32s = MemoryMarshal.Cast<byte, uint>(data);
        foreach (var value in uint32s)
        {
            states |= value switch
            {
                1 => XdgToplevelStates.Maximized,
                2 => XdgToplevelStates.Fullscreen,
                3 => XdgToplevelStates.Resizing,
                4 => XdgToplevelStates.Activated,
                5 => XdgToplevelStates.TiledLeft,
                6 => XdgToplevelStates.TiledRight,
                7 => XdgToplevelStates.TiledTop,
                8 => XdgToplevelStates.TiledBottom,
                9 => XdgToplevelStates.Suspended,
                _ => XdgToplevelStates.None,
            };
        }
        return states;
    }
}
