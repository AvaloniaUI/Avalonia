using global::Avalonia.Input;
using global::Avalonia.Input.Raw;
using global::Avalonia.Win32.Input;
using Windows.System;
using Windows.UI.Core;

namespace Avalonia.WinUI;

internal static class WinUIKeyInterop
{
    /// <summary>
    /// Resolves WinUI key-event metadata to Avalonia's <see cref="Key"/>,
    /// <see cref="PhysicalKey"/> and key symbol, using the shared Win32 mapping
    /// tables in <see cref="KeyInterop"/>. WinUI's <c>KeyRoutedEventArgs</c>
    /// exposes virtual-key + scan-code + extended flag — packing those into the
    /// same lParam layout the Win32 backend uses lets us reuse all of its
    /// mapping logic (Numpad disambiguation, left/right modifier split, etc.).
    /// </summary>
    public static (Key Key, PhysicalKey PhysicalKey, string? KeySymbol) Resolve(
        VirtualKey virtualKey, CorePhysicalKeyStatus status)
    {
        var keyData = EncodeKeyData(status);
        var vk = (int)virtualKey;
        var key = KeyInterop.KeyFromVirtualKey(vk, keyData);
        var physical = KeyInterop.PhysicalKeyFromVirtualKey(vk, keyData);
        var symbol = KeyInterop.GetKeySymbolFromVirtualKey(vk);
        return (key, physical, symbol);
    }

    public static Key KeyFromVirtualKey(VirtualKey virtualKey)
    {
        // Fallback used by callers that don't have KeyStatus (rare).
        return KeyInterop.KeyFromVirtualKey((int)virtualKey, 0);
    }

    public static RawInputModifiers ModifiersFromVirtualKeyModifiers(VirtualKeyModifiers modifiers)
    {
        var result = RawInputModifiers.None;
        if (modifiers.HasFlag(VirtualKeyModifiers.Control))
            result |= RawInputModifiers.Control;
        if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
            result |= RawInputModifiers.Shift;
        if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
            result |= RawInputModifiers.Alt;
        if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
            result |= RawInputModifiers.Meta;
        return result;
    }

    private static int EncodeKeyData(CorePhysicalKeyStatus status)
    {
        // Mirror the WM_KEYDOWN lParam layout that Win32's KeyInterop expects:
        //   bits 16-23 : scan code (low byte)
        //   bit 24     : extended-key flag
        // Repeat count, context, previous/transition bits are unused by the
        // key/physical-key/symbol resolvers, so leave them zero.
        var data = (int)((status.ScanCode & 0xFF) << 16);
        if (status.IsExtendedKey)
            data |= 1 << 24;
        return data;
    }
}
