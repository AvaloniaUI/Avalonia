using global::Avalonia.Input;
using global::Avalonia.Input.Raw;
using global::Avalonia.Win32.Input;
using Windows.System;

namespace Avalonia.WinUI;

internal static class WinUIKeyInterop
{
    public static Key KeyFromVirtualKey(VirtualKey virtualKey)
    {
        // WinUI VirtualKey enum values match Win32 VK_ constants.
        // Pass keyData=0; modifier disambiguation (L/R shift etc.) won't work
        // via scan codes, but WinUI doesn't distinguish those anyway.
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
}
