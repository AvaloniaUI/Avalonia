using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

internal static class MaliitKeyMapper
{
    internal static bool TryMap(InputKey key, out X11InputMethodForwardedKey result)
    {
        // Qt::Key values are not X11 keysyms.
        var symbol = key.Key switch
        {
            0x01000000 => 0xff1b, // Escape
            0x01000001 => 0xff09, // Tab
            0x01000002 => 0xfe20, // Backtab
            0x01000003 => 0xff08, // Backspace
            0x01000004 => 0xff0d, // Return
            0x01000005 => 0xff8d, // Keypad Enter
            0x01000006 => 0xff63, // Insert
            0x01000007 => 0xffff, // Delete
            0x01000010 => 0xff50, // Home
            0x01000011 => 0xff57, // End
            0x01000012 => 0xff51, // Left
            0x01000013 => 0xff52, // Up
            0x01000014 => 0xff53, // Right
            0x01000015 => 0xff54, // Down
            0x01000016 => 0xff55, // Page Up
            0x01000017 => 0xff56, // Page Down
            >= 0x20 and <= 0xff => key.Key,
            >= 0x100 and <= 0x10ffff => 0x01000000 | key.Key,
            _ => 0
        };
        var modifiers = KeyModifiers.None;
        if ((key.Modifiers & 0x02000000) != 0) modifiers |= KeyModifiers.Shift;
        if ((key.Modifiers & 0x04000000) != 0) modifiers |= KeyModifiers.Control;
        if ((key.Modifiers & 0x08000000) != 0) modifiers |= KeyModifiers.Alt;
        if ((key.Modifiers & 0x10000000) != 0) modifiers |= KeyModifiers.Meta;
        result = new X11InputMethodForwardedKey
        {
            KeyVal = symbol,
            Modifiers = modifiers,
            Type = key.Type == 6 ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
            Text = key.Text,
            WithText = key.Text.Length > 0 && (modifiers & ~KeyModifiers.Shift) == 0
        };
        return symbol != 0 && key.RequestType != 1 && key.Type is 6 or 7;
    }
}
