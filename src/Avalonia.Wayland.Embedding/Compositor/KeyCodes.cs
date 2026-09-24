using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.X11;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Maps Avalonia <see cref="PhysicalKey"/> to Linux evdev keycodes for wl_keyboard.key (the value the client's
/// xkb keymap resolves to a keysym). 0 = unmapped (skip).
/// </summary>
/// <remarks>
/// Rather than maintain a parallel table, this reverses the X11 backend's upstream-maintained
/// <see cref="X11KeyTransform.PhysicalKeyFromScanCode"/> map (compiled into this assembly). X11 keycodes are
/// the XKB scan codes, which are Linux evdev keycodes plus a fixed offset of 8, so
/// <c>evdev = x11ScanCode - 8</c>. Built once; pure thereafter.
/// </remarks>
internal static class KeyCodes
{
    private const int XkbEvdevOffset = 8;

    private static readonly Dictionary<PhysicalKey, uint> s_evdevFromPhysicalKey = BuildMap();

    private static Dictionary<PhysicalKey, uint> BuildMap()
    {
        // X11 scan codes occupy a single byte (1..255). Probe each through the upstream table and invert it,
        // keeping the first scan code that maps to a given PhysicalKey (the table is 1:1 in this direction).
        var map = new Dictionary<PhysicalKey, uint>(256);
        for (var scanCode = XkbEvdevOffset + 1; scanCode <= 255; scanCode++)
        {
            var physicalKey = X11KeyTransform.PhysicalKeyFromScanCode(scanCode);
            if (physicalKey != PhysicalKey.None)
                map.TryAdd(physicalKey, (uint)(scanCode - XkbEvdevOffset));
        }

        return map;
    }

    public static uint ToEvdev(PhysicalKey key) =>
        s_evdevFromPhysicalKey.TryGetValue(key, out var code) ? code : 0;
}
