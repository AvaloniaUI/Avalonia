using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.Wayland;

/// <summary>
/// Maps evdev keycodes and XKB keysyms to Avalonia Key and PhysicalKey values.
/// </summary>
internal static class XkbKeyTransform
{
    // evdev keycode → Avalonia Key
    // Based on linux/input-event-codes.h
    private static readonly Dictionary<uint, Key> s_keyFromEvdev = new(128)
    {
        { 1, Key.Escape },
        { 2, Key.D1 },
        { 3, Key.D2 },
        { 4, Key.D3 },
        { 5, Key.D4 },
        { 6, Key.D5 },
        { 7, Key.D6 },
        { 8, Key.D7 },
        { 9, Key.D8 },
        { 10, Key.D9 },
        { 11, Key.D0 },
        { 12, Key.OemMinus },
        { 13, Key.OemPlus },
        { 14, Key.Back },
        { 15, Key.Tab },
        { 16, Key.Q },
        { 17, Key.W },
        { 18, Key.E },
        { 19, Key.R },
        { 20, Key.T },
        { 21, Key.Y },
        { 22, Key.U },
        { 23, Key.I },
        { 24, Key.O },
        { 25, Key.P },
        { 26, Key.OemOpenBrackets },
        { 27, Key.OemCloseBrackets },
        { 28, Key.Return },
        { 29, Key.LeftCtrl },
        { 30, Key.A },
        { 31, Key.S },
        { 32, Key.D },
        { 33, Key.F },
        { 34, Key.G },
        { 35, Key.H },
        { 36, Key.J },
        { 37, Key.K },
        { 38, Key.L },
        { 39, Key.OemSemicolon },
        { 40, Key.OemQuotes },
        { 41, Key.OemTilde },
        { 42, Key.LeftShift },
        { 43, Key.OemPipe },
        { 44, Key.Z },
        { 45, Key.X },
        { 46, Key.C },
        { 47, Key.V },
        { 48, Key.B },
        { 49, Key.N },
        { 50, Key.M },
        { 51, Key.OemComma },
        { 52, Key.OemPeriod },
        { 53, Key.Oem2 },       // Slash
        { 54, Key.RightShift },
        { 55, Key.Multiply },   // KP_Multiply
        { 56, Key.LeftAlt },
        { 57, Key.Space },
        { 58, Key.CapsLock },
        { 59, Key.F1 },
        { 60, Key.F2 },
        { 61, Key.F3 },
        { 62, Key.F4 },
        { 63, Key.F5 },
        { 64, Key.F6 },
        { 65, Key.F7 },
        { 66, Key.F8 },
        { 67, Key.F9 },
        { 68, Key.F10 },
        { 69, Key.NumLock },
        { 70, Key.Scroll },
        { 71, Key.NumPad7 },
        { 72, Key.NumPad8 },
        { 73, Key.NumPad9 },
        { 74, Key.Subtract },
        { 75, Key.NumPad4 },
        { 76, Key.NumPad5 },
        { 77, Key.NumPad6 },
        { 78, Key.Add },
        { 79, Key.NumPad1 },
        { 80, Key.NumPad2 },
        { 81, Key.NumPad3 },
        { 82, Key.NumPad0 },
        { 83, Key.Decimal },
        { 87, Key.F11 },
        { 88, Key.F12 },
        { 96, Key.Return },     // KP_Enter
        { 97, Key.RightCtrl },
        { 98, Key.Divide },     // KP_Divide
        { 99, Key.Print },      // SysRq / PrintScreen
        { 100, Key.RightAlt },
        { 102, Key.Home },
        { 103, Key.Up },
        { 104, Key.Prior },     // PageUp
        { 105, Key.Left },
        { 106, Key.Right },
        { 107, Key.End },
        { 108, Key.Down },
        { 109, Key.PageDown },
        { 110, Key.Insert },
        { 111, Key.Delete },
        { 113, Key.None },      // Mute (media)
        { 114, Key.None },      // VolumeDown
        { 115, Key.None },      // VolumeUp
        { 119, Key.Pause },
        { 125, Key.LWin },
        { 126, Key.RWin },
        { 127, Key.Apps },      // Compose / Menu
        { 183, Key.F13 },
        { 184, Key.F14 },
        { 185, Key.F15 },
        { 186, Key.F16 },
        { 187, Key.F17 },
        { 188, Key.F18 },
        { 189, Key.F19 },
        { 190, Key.F20 },
        { 191, Key.F21 },
        { 192, Key.F22 },
        { 193, Key.F23 },
        { 194, Key.F24 },
    };

    // evdev keycode → PhysicalKey
    // Adapted from X11KeyTransform.s_physicalKeyFromScanCode by subtracting 8 from X11 scancodes.
    private static readonly Dictionary<uint, PhysicalKey> s_physicalKeyFromEvdev = new(162)
    {
        // Writing System Keys
        { 41, PhysicalKey.Backquote },
        { 43, PhysicalKey.Backslash },
        { 26, PhysicalKey.BracketLeft },
        { 27, PhysicalKey.BracketRight },
        { 51, PhysicalKey.Comma },
        { 11, PhysicalKey.Digit0 },
        { 2, PhysicalKey.Digit1 },
        { 3, PhysicalKey.Digit2 },
        { 4, PhysicalKey.Digit3 },
        { 5, PhysicalKey.Digit4 },
        { 6, PhysicalKey.Digit5 },
        { 7, PhysicalKey.Digit6 },
        { 8, PhysicalKey.Digit7 },
        { 9, PhysicalKey.Digit8 },
        { 10, PhysicalKey.Digit9 },
        { 13, PhysicalKey.Equal },
        { 86, PhysicalKey.IntlBackslash },
        { 89, PhysicalKey.IntlRo },
        { 124, PhysicalKey.IntlYen },
        { 30, PhysicalKey.A },
        { 48, PhysicalKey.B },
        { 46, PhysicalKey.C },
        { 32, PhysicalKey.D },
        { 18, PhysicalKey.E },
        { 33, PhysicalKey.F },
        { 34, PhysicalKey.G },
        { 35, PhysicalKey.H },
        { 23, PhysicalKey.I },
        { 36, PhysicalKey.J },
        { 37, PhysicalKey.K },
        { 38, PhysicalKey.L },
        { 50, PhysicalKey.M },
        { 49, PhysicalKey.N },
        { 24, PhysicalKey.O },
        { 25, PhysicalKey.P },
        { 16, PhysicalKey.Q },
        { 19, PhysicalKey.R },
        { 31, PhysicalKey.S },
        { 20, PhysicalKey.T },
        { 22, PhysicalKey.U },
        { 47, PhysicalKey.V },
        { 17, PhysicalKey.W },
        { 45, PhysicalKey.X },
        { 21, PhysicalKey.Y },
        { 44, PhysicalKey.Z },
        { 12, PhysicalKey.Minus },
        { 52, PhysicalKey.Period },
        { 40, PhysicalKey.Quote },
        { 39, PhysicalKey.Semicolon },
        { 53, PhysicalKey.Slash },

        // Functional Keys
        { 56, PhysicalKey.AltLeft },
        { 100, PhysicalKey.AltRight },
        { 14, PhysicalKey.Backspace },
        { 58, PhysicalKey.CapsLock },
        { 127, PhysicalKey.ContextMenu },
        { 29, PhysicalKey.ControlLeft },
        { 97, PhysicalKey.ControlRight },
        { 28, PhysicalKey.Enter },
        { 125, PhysicalKey.MetaLeft },
        { 126, PhysicalKey.MetaRight },
        { 42, PhysicalKey.ShiftLeft },
        { 54, PhysicalKey.ShiftRight },
        { 57, PhysicalKey.Space },
        { 15, PhysicalKey.Tab },
        { 92, PhysicalKey.Convert },
        { 93, PhysicalKey.KanaMode },
        { 122, PhysicalKey.Lang1 },
        { 123, PhysicalKey.Lang2 },
        { 90, PhysicalKey.Lang3 },
        { 91, PhysicalKey.Lang4 },
        { 85, PhysicalKey.Lang5 },
        { 94, PhysicalKey.NonConvert },

        // Control Pad Section
        { 111, PhysicalKey.Delete },
        { 107, PhysicalKey.End },
        { 138, PhysicalKey.Help },
        { 102, PhysicalKey.Home },
        { 110, PhysicalKey.Insert },
        { 109, PhysicalKey.PageDown },
        { 104, PhysicalKey.PageUp },

        // Arrow Pad Section
        { 108, PhysicalKey.ArrowDown },
        { 105, PhysicalKey.ArrowLeft },
        { 106, PhysicalKey.ArrowRight },
        { 103, PhysicalKey.ArrowUp },

        // Numpad Section
        { 69, PhysicalKey.NumLock },
        { 82, PhysicalKey.NumPad0 },
        { 79, PhysicalKey.NumPad1 },
        { 80, PhysicalKey.NumPad2 },
        { 81, PhysicalKey.NumPad3 },
        { 75, PhysicalKey.NumPad4 },
        { 76, PhysicalKey.NumPad5 },
        { 77, PhysicalKey.NumPad6 },
        { 71, PhysicalKey.NumPad7 },
        { 72, PhysicalKey.NumPad8 },
        { 73, PhysicalKey.NumPad9 },
        { 78, PhysicalKey.NumPadAdd },
        { 121, PhysicalKey.NumPadComma },
        { 83, PhysicalKey.NumPadDecimal },
        { 98, PhysicalKey.NumPadDivide },
        { 96, PhysicalKey.NumPadEnter },
        { 117, PhysicalKey.NumPadEqual },
        { 55, PhysicalKey.NumPadMultiply },
        { 179, PhysicalKey.NumPadParenLeft },
        { 180, PhysicalKey.NumPadParenRight },
        { 74, PhysicalKey.NumPadSubtract },

        // Function Section
        { 1, PhysicalKey.Escape },
        { 59, PhysicalKey.F1 },
        { 60, PhysicalKey.F2 },
        { 61, PhysicalKey.F3 },
        { 62, PhysicalKey.F4 },
        { 63, PhysicalKey.F5 },
        { 64, PhysicalKey.F6 },
        { 65, PhysicalKey.F7 },
        { 66, PhysicalKey.F8 },
        { 67, PhysicalKey.F9 },
        { 68, PhysicalKey.F10 },
        { 87, PhysicalKey.F11 },
        { 88, PhysicalKey.F12 },
        { 183, PhysicalKey.F13 },
        { 184, PhysicalKey.F14 },
        { 185, PhysicalKey.F15 },
        { 186, PhysicalKey.F16 },
        { 187, PhysicalKey.F17 },
        { 188, PhysicalKey.F18 },
        { 189, PhysicalKey.F19 },
        { 190, PhysicalKey.F20 },
        { 191, PhysicalKey.F21 },
        { 192, PhysicalKey.F22 },
        { 193, PhysicalKey.F23 },
        { 194, PhysicalKey.F24 },
        { 99, PhysicalKey.PrintScreen },
        { 70, PhysicalKey.ScrollLock },
        { 119, PhysicalKey.Pause },

        // Media Keys
        { 158, PhysicalKey.BrowserBack },
        { 156, PhysicalKey.BrowserFavorites },
        { 159, PhysicalKey.BrowserForward },
        { 172, PhysicalKey.BrowserHome },
        { 173, PhysicalKey.BrowserRefresh },
        { 217, PhysicalKey.BrowserSearch },
        { 128, PhysicalKey.BrowserStop },
        { 161, PhysicalKey.Eject },
        { 144, PhysicalKey.LaunchApp1 },
        { 140, PhysicalKey.LaunchApp2 },
        { 155, PhysicalKey.LaunchMail },
        { 164, PhysicalKey.MediaPlayPause },
        { 171, PhysicalKey.MediaSelect },
        { 166, PhysicalKey.MediaStop },
        { 163, PhysicalKey.MediaTrackNext },
        { 165, PhysicalKey.MediaTrackPrevious },
        { 116, PhysicalKey.Power },
        { 142, PhysicalKey.Sleep },
        { 114, PhysicalKey.AudioVolumeDown },
        { 113, PhysicalKey.AudioVolumeMute },
        { 115, PhysicalKey.AudioVolumeUp },
        { 143, PhysicalKey.WakeUp },

        // Legacy Keys
        { 129, PhysicalKey.Again },
        { 133, PhysicalKey.Copy },
        { 137, PhysicalKey.Cut },
        { 136, PhysicalKey.Find },
        { 134, PhysicalKey.Open },
        { 135, PhysicalKey.Paste },
        { 132, PhysicalKey.Select },
        { 131, PhysicalKey.Undo }
    };

    public static Key KeyFromEvdev(uint evdevCode)
        => s_keyFromEvdev.TryGetValue(evdevCode, out var result) ? result : Key.None;

    public static PhysicalKey PhysicalKeyFromEvdev(uint evdevCode)
        => s_physicalKeyFromEvdev.TryGetValue(evdevCode, out var result) ? result : PhysicalKey.None;

    public static Key KeyFromKeysym(uint keysym)
        => s_keyFromKeysym.TryGetValue(keysym, out var result) ? result : Key.None;

    /// <summary>
    /// Full key resolution with non-latin keyboard fallback.
    /// Mirrors X11's LookupKey/LookUpKeyXkb behavior:
    /// 1. Try current layout keysym → Key
    /// 2. Digit keys always forced to QWERTY (matching Windows/macOS)
    /// 3. If Key.None, cycle other layout groups to find a latin key
    /// 4. Ultimate fallback to PhysicalKey.ToQwertyKey()
    /// </summary>
    public static (Key key, string? keySymbol) ResolveKeyWithFallback(
        XkbCommonKeymap keymap, uint evdevKeycode, PhysicalKey physicalKey)
    {
        var (keysym, text) = keymap.ResolveKey(evdevKeycode);
        var keySymbol = FilterKeySymbol(text);

        // Always use digit keys from QWERTY, matching Windows/macOS
        if (physicalKey is >= PhysicalKey.Digit0 and <= PhysicalKey.Digit9)
            return (physicalKey.ToQwertyKey(), keySymbol);

        var key = KeyFromKeysym(keysym);
        if (key != Key.None)
            return (key, keySymbol);

        // Non-latin fallback: cycle through other layout groups
        var layoutCount = keymap.LayoutCount;
        for (uint layout = 0; layout < layoutCount; layout++)
        {
            var groupKeysym = keymap.FindKeysymInLayout(evdevKeycode, layout);
            if (groupKeysym != 0)
            {
                key = KeyFromKeysym(groupKeysym);
                if (key != Key.None)
                    return (key, keySymbol);
            }
        }

        // Ultimate fallback: QWERTY physical key mapping
        if (keysym != 0)
            return (physicalKey.ToQwertyKey(), keySymbol);

        return (Key.None, keySymbol);
    }

    internal static string? FilterKeySymbol(string? text)
    {
        if (text is null || text.Length == 0)
            return null;

        if (text.Length == 1)
        {
            var c = text[0];
            // Reject control characters and DEL
            if (c < ' ' && c != '\b' && c != '\t' && c != '\r' && c != '\x1b')
                return null;
            if (c == '\x7f')
                return null;
        }

        return text;
    }

    // XKB keysym → Avalonia Key
    // Keysym values are identical to X11 keysym values (from xkbcommon-keysyms.h / keysymdef.h)
    private static readonly Dictionary<uint, Key> s_keyFromKeysym = new(200)
    {
        // Function/control keys (0xff00+ range)
        { 0xff69, Key.Cancel },       // XKB_KEY_Cancel
        { 0xff08, Key.Back },         // XKB_KEY_BackSpace
        { 0xff09, Key.Tab },          // XKB_KEY_Tab
        { 0xff0a, Key.LineFeed },     // XKB_KEY_Linefeed
        { 0xff0b, Key.Clear },        // XKB_KEY_Clear
        { 0xff0d, Key.Return },       // XKB_KEY_Return
        { 0xff8d, Key.Return },       // XKB_KEY_KP_Enter
        { 0xff13, Key.Pause },        // XKB_KEY_Pause
        { 0xffe5, Key.CapsLock },     // XKB_KEY_Caps_Lock
        { 0xff1b, Key.Escape },       // XKB_KEY_Escape
        { 0x0020, Key.Space },        // XKB_KEY_space
        { 0xff55, Key.Prior },        // XKB_KEY_Prior / Page_Up
        { 0xff9a, Key.Prior },        // XKB_KEY_KP_Prior
        { 0xff56, Key.PageDown },     // XKB_KEY_Next / Page_Down
        { 0xff9b, Key.PageDown },     // XKB_KEY_KP_Next
        { 0xff57, Key.End },          // XKB_KEY_End
        { 0xff9c, Key.End },          // XKB_KEY_KP_End
        { 0xff50, Key.Home },         // XKB_KEY_Home
        { 0xff95, Key.Home },         // XKB_KEY_KP_Home
        { 0xff51, Key.Left },         // XKB_KEY_Left
        { 0xff96, Key.Left },         // XKB_KEY_KP_Left
        { 0xff52, Key.Up },           // XKB_KEY_Up
        { 0xff97, Key.Up },           // XKB_KEY_KP_Up
        { 0xff53, Key.Right },        // XKB_KEY_Right
        { 0xff98, Key.Right },        // XKB_KEY_KP_Right
        { 0xff54, Key.Down },         // XKB_KEY_Down
        { 0xff99, Key.Down },         // XKB_KEY_KP_Down
        { 0xff60, Key.Select },       // XKB_KEY_Select
        { 0xff61, Key.Print },        // XKB_KEY_Print
        { 0xff62, Key.Execute },      // XKB_KEY_Execute
        { 0xff63, Key.Insert },       // XKB_KEY_Insert
        { 0xff9e, Key.Insert },       // XKB_KEY_KP_Insert
        { 0xffff, Key.Delete },       // XKB_KEY_Delete
        { 0xff9f, Key.Delete },       // XKB_KEY_KP_Delete
        { 0xff6a, Key.Help },         // XKB_KEY_Help

        // Latin uppercase (A-Z, 0x41-0x5a)
        { 0x0041, Key.A },
        { 0x0042, Key.B },
        { 0x0043, Key.C },
        { 0x0044, Key.D },
        { 0x0045, Key.E },
        { 0x0046, Key.F },
        { 0x0047, Key.G },
        { 0x0048, Key.H },
        { 0x0049, Key.I },
        { 0x004a, Key.J },
        { 0x004b, Key.K },
        { 0x004c, Key.L },
        { 0x004d, Key.M },
        { 0x004e, Key.N },
        { 0x004f, Key.O },
        { 0x0050, Key.P },
        { 0x0051, Key.Q },
        { 0x0052, Key.R },
        { 0x0053, Key.S },
        { 0x0054, Key.T },
        { 0x0055, Key.U },
        { 0x0056, Key.V },
        { 0x0057, Key.W },
        { 0x0058, Key.X },
        { 0x0059, Key.Y },
        { 0x005a, Key.Z },

        // Latin lowercase (a-z, 0x61-0x7a)
        { 0x0061, Key.A },
        { 0x0062, Key.B },
        { 0x0063, Key.C },
        { 0x0064, Key.D },
        { 0x0065, Key.E },
        { 0x0066, Key.F },
        { 0x0067, Key.G },
        { 0x0068, Key.H },
        { 0x0069, Key.I },
        { 0x006a, Key.J },
        { 0x006b, Key.K },
        { 0x006c, Key.L },
        { 0x006d, Key.M },
        { 0x006e, Key.N },
        { 0x006f, Key.O },
        { 0x0070, Key.P },
        { 0x0071, Key.Q },
        { 0x0072, Key.R },
        { 0x0073, Key.S },
        { 0x0074, Key.T },
        { 0x0075, Key.U },
        { 0x0076, Key.V },
        { 0x0077, Key.W },
        { 0x0078, Key.X },
        { 0x0079, Key.Y },
        { 0x007a, Key.Z },

        // Super / Meta / Apps
        { 0xffeb, Key.LWin },         // XKB_KEY_Super_L
        { 0xffec, Key.RWin },         // XKB_KEY_Super_R
        { 0xff67, Key.Apps },         // XKB_KEY_Menu

        // Numpad
        { 0xffb0, Key.NumPad0 },      // XKB_KEY_KP_0
        { 0xffb1, Key.NumPad1 },
        { 0xffb2, Key.NumPad2 },
        { 0xffb3, Key.NumPad3 },
        { 0xffb4, Key.NumPad4 },
        { 0xffb5, Key.NumPad5 },
        { 0xffb6, Key.NumPad6 },
        { 0xffb7, Key.NumPad7 },
        { 0xffb8, Key.NumPad8 },
        { 0xffb9, Key.NumPad9 },      // XKB_KEY_KP_9
        { 0x002a, Key.Multiply },     // asterisk
        { 0xffaa, Key.Multiply },     // XKB_KEY_KP_Multiply
        { 0xffab, Key.Add },          // XKB_KEY_KP_Add
        { 0xffad, Key.Subtract },     // XKB_KEY_KP_Subtract
        { 0xffae, Key.Decimal },      // XKB_KEY_KP_Decimal
        { 0xffaf, Key.Divide },       // XKB_KEY_KP_Divide

        // Function keys
        { 0xffbe, Key.F1 },
        { 0xffbf, Key.F2 },
        { 0xffc0, Key.F3 },
        { 0xffc1, Key.F4 },
        { 0xffc2, Key.F5 },
        { 0xffc3, Key.F6 },
        { 0xffc4, Key.F7 },
        { 0xffc5, Key.F8 },
        { 0xffc6, Key.F9 },
        { 0xffc7, Key.F10 },
        { 0xffc8, Key.F11 },
        { 0xffc9, Key.F12 },
        { 0xffca, Key.F13 },
        { 0xffcb, Key.F14 },
        { 0xffcc, Key.F15 },
        { 0xffcd, Key.F16 },
        { 0xffce, Key.F17 },
        { 0xffcf, Key.F18 },
        { 0xffd0, Key.F19 },
        { 0xffd1, Key.F20 },
        { 0xffd2, Key.F21 },
        { 0xffd3, Key.F22 },
        { 0xffd4, Key.F23 },
        { 0xffd5, Key.F24 },

        // Lock / modifier keys
        { 0xff7f, Key.NumLock },      // XKB_KEY_Num_Lock
        { 0xff14, Key.Scroll },       // XKB_KEY_Scroll_Lock
        { 0xffe1, Key.LeftShift },    // XKB_KEY_Shift_L
        { 0xffe2, Key.RightShift },   // XKB_KEY_Shift_R
        { 0xffe3, Key.LeftCtrl },     // XKB_KEY_Control_L
        { 0xffe4, Key.RightCtrl },    // XKB_KEY_Control_R
        { 0xffe9, Key.LeftAlt },      // XKB_KEY_Alt_L
        { 0xffea, Key.RightAlt },     // XKB_KEY_Alt_R

        // Punctuation / OEM keys
        { 0x002d, Key.OemMinus },     // minus
        { 0x005f, Key.OemMinus },     // underscore
        { 0x002b, Key.OemPlus },      // plus
        { 0x003d, Key.OemPlus },      // equal
        { 0x005b, Key.OemOpenBrackets },  // bracketleft
        { 0x007b, Key.OemOpenBrackets },  // braceleft
        { 0x005d, Key.OemCloseBrackets }, // bracketright
        { 0x007d, Key.OemCloseBrackets }, // braceright
        { 0x005c, Key.OemPipe },      // backslash
        { 0x007c, Key.OemPipe },      // bar
        { 0x003b, Key.OemSemicolon }, // semicolon
        { 0x003a, Key.OemSemicolon }, // colon
        { 0x0027, Key.OemQuotes },    // apostrophe
        { 0x0022, Key.OemQuotes },    // quotedbl
        { 0x002c, Key.OemComma },     // comma
        { 0x003c, Key.OemComma },     // less
        { 0x002e, Key.OemPeriod },    // period
        { 0x003e, Key.OemPeriod },    // greater
        { 0x002f, Key.Oem2 },        // slash
        { 0x003f, Key.Oem2 },        // question
        { 0x0060, Key.OemTilde },     // grave
        { 0x007e, Key.OemTilde },     // asciitilde

        // Digits (0-9)
        { 0x0031, Key.D1 },
        { 0x0032, Key.D2 },
        { 0x0033, Key.D3 },
        { 0x0034, Key.D4 },
        { 0x0035, Key.D5 },
        { 0x0036, Key.D6 },
        { 0x0037, Key.D7 },
        { 0x0038, Key.D8 },
        { 0x0039, Key.D9 },
        { 0x0030, Key.D0 },
    };
}
