using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.X11
{
    internal static class X11KeyTransform
    {
        // X scan code to physical key map.
        // https://github.com/chromium/chromium/blob/main/ui/events/keycodes/dom/dom_code_data.inc
        // This list has the same order as the PhysicalKey enum.
        private static readonly Dictionary<byte, PhysicalKey> s_physicalKeyFromScanCode = new(162)
        {
            // Writing System Keys
            { 0x31, PhysicalKey.Backquote },
            { 0x33, PhysicalKey.Backslash },
            { 0x22, PhysicalKey.BracketLeft },
            { 0x23, PhysicalKey.BracketRight },
            { 0x3B, PhysicalKey.Comma },
            { 0x13, PhysicalKey.Digit0 },
            { 0x0A, PhysicalKey.Digit1 },
            { 0x0B, PhysicalKey.Digit2 },
            { 0x0C, PhysicalKey.Digit3 },
            { 0x0D, PhysicalKey.Digit4 },
            { 0x0E, PhysicalKey.Digit5 },
            { 0x0F, PhysicalKey.Digit6 },
            { 0x10, PhysicalKey.Digit7 },
            { 0x11, PhysicalKey.Digit8 },
            { 0x12, PhysicalKey.Digit9 },
            { 0x15, PhysicalKey.Equal },
            { 0x5E, PhysicalKey.IntlBackslash },
            { 0x61, PhysicalKey.IntlRo },
            { 0x84, PhysicalKey.IntlYen },
            { 0x26, PhysicalKey.A },
            { 0x38, PhysicalKey.B },
            { 0x36, PhysicalKey.C },
            { 0x28, PhysicalKey.D },
            { 0x1A, PhysicalKey.E },
            { 0x29, PhysicalKey.F },
            { 0x2A, PhysicalKey.G },
            { 0x2B, PhysicalKey.H },
            { 0x1F, PhysicalKey.I },
            { 0x2C, PhysicalKey.J },
            { 0x2D, PhysicalKey.K },
            { 0x2E, PhysicalKey.L },
            { 0x3A, PhysicalKey.M },
            { 0x39, PhysicalKey.N },
            { 0x20, PhysicalKey.O },
            { 0x21, PhysicalKey.P },
            { 0x18, PhysicalKey.Q },
            { 0x1B, PhysicalKey.R },
            { 0x27, PhysicalKey.S },
            { 0x1C, PhysicalKey.T },
            { 0x1E, PhysicalKey.U },
            { 0x37, PhysicalKey.V },
            { 0x19, PhysicalKey.W },
            { 0x35, PhysicalKey.X },
            { 0x1D, PhysicalKey.Y },
            { 0x34, PhysicalKey.Z },
            { 0x14, PhysicalKey.Minus },
            { 0x3C, PhysicalKey.Period },
            { 0x30, PhysicalKey.Quote },
            { 0x2F, PhysicalKey.Semicolon },
            { 0x3D, PhysicalKey.Slash },

            // Functional Keys
            { 0x40, PhysicalKey.AltLeft },
            { 0x6C, PhysicalKey.AltRight },
            { 0x16, PhysicalKey.Backspace },
            { 0x42, PhysicalKey.CapsLock },
            { 0x87, PhysicalKey.ContextMenu },
            { 0x25, PhysicalKey.ControlLeft },
            { 0x69, PhysicalKey.ControlRight },
            { 0x24, PhysicalKey.Enter },
            { 0x85, PhysicalKey.MetaLeft },
            { 0x86, PhysicalKey.MetaRight },
            { 0x32, PhysicalKey.ShiftLeft },
            { 0x3E, PhysicalKey.ShiftRight },
            { 0x41, PhysicalKey.Space },
            { 0x17, PhysicalKey.Tab },
            { 0x64, PhysicalKey.Convert },
            { 0x65, PhysicalKey.KanaMode },
            { 0x82, PhysicalKey.Lang1 },
            { 0x83, PhysicalKey.Lang2 },
            { 0x62, PhysicalKey.Lang3 },
            { 0x63, PhysicalKey.Lang4 },
            { 0x5D, PhysicalKey.Lang5 },
            { 0x66, PhysicalKey.NonConvert },

            // Control Pad Section
            { 0x77, PhysicalKey.Delete },
            { 0x73, PhysicalKey.End },
            { 0x92, PhysicalKey.Help },
            { 0x6E, PhysicalKey.Home },
            { 0x76, PhysicalKey.Insert },
            { 0x75, PhysicalKey.PageDown },
            { 0x70, PhysicalKey.PageUp },

            // Arrow Pad Section
            { 0x74, PhysicalKey.ArrowDown },
            { 0x71, PhysicalKey.ArrowLeft },
            { 0x72, PhysicalKey.ArrowRight },
            { 0x6F, PhysicalKey.ArrowUp },

            // Numpad Section
            { 0x4D, PhysicalKey.NumLock },
            { 0x5A, PhysicalKey.NumPad0 },
            { 0x57, PhysicalKey.NumPad1 },
            { 0x58, PhysicalKey.NumPad2 },
            { 0x59, PhysicalKey.NumPad3 },
            { 0x53, PhysicalKey.NumPad4 },
            { 0x54, PhysicalKey.NumPad5 },
            { 0x55, PhysicalKey.NumPad6 },
            { 0x4F, PhysicalKey.NumPad7 },
            { 0x50, PhysicalKey.NumPad8 },
            { 0x51, PhysicalKey.NumPad9 },
            { 0x56, PhysicalKey.NumPadAdd },
            //{     , PhysicalKey.NumPadClear },
            { 0x81, PhysicalKey.NumPadComma },
            { 0x5B, PhysicalKey.NumPadDecimal },
            { 0x6A, PhysicalKey.NumPadDivide },
            { 0x68, PhysicalKey.NumPadEnter },
            { 0x7D, PhysicalKey.NumPadEqual },
            { 0x3F, PhysicalKey.NumPadMultiply },
            { 0xBB, PhysicalKey.NumPadParenLeft },
            { 0xBC, PhysicalKey.NumPadParenRight },
            { 0x52, PhysicalKey.NumPadSubtract },

            // Function Section
            { 0x09, PhysicalKey.Escape },
            { 0x43, PhysicalKey.F1 },
            { 0x44, PhysicalKey.F2 },
            { 0x45, PhysicalKey.F3 },
            { 0x46, PhysicalKey.F4 },
            { 0x47, PhysicalKey.F5 },
            { 0x48, PhysicalKey.F6 },
            { 0x49, PhysicalKey.F7 },
            { 0x4A, PhysicalKey.F8 },
            { 0x4B, PhysicalKey.F9 },
            { 0x4C, PhysicalKey.F10 },
            { 0x5F, PhysicalKey.F11 },
            { 0x60, PhysicalKey.F12 },
            { 0xBF, PhysicalKey.F13 },
            { 0xC0, PhysicalKey.F14 },
            { 0xC1, PhysicalKey.F15 },
            { 0xC2, PhysicalKey.F16 },
            { 0xC3, PhysicalKey.F17 },
            { 0xC4, PhysicalKey.F18 },
            { 0xC5, PhysicalKey.F19 },
            { 0xC6, PhysicalKey.F20 },
            { 0xC7, PhysicalKey.F21 },
            { 0xC8, PhysicalKey.F22 },
            { 0xC9, PhysicalKey.F23 },
            { 0xCA, PhysicalKey.F24 },
            { 0x6B, PhysicalKey.PrintScreen },
            { 0x4E, PhysicalKey.ScrollLock },
            { 0x7F, PhysicalKey.Pause },

            // Media Keys
            { 0xA6, PhysicalKey.BrowserBack },
            { 0xA4, PhysicalKey.BrowserFavorites },
            { 0xA7, PhysicalKey.BrowserForward },
            { 0xB4, PhysicalKey.BrowserHome },
            { 0xB5, PhysicalKey.BrowserRefresh },
            { 0xE1, PhysicalKey.BrowserSearch },
            { 0x88, PhysicalKey.BrowserStop },
            { 0xA9, PhysicalKey.Eject },
            { 0x98, PhysicalKey.LaunchApp1 },
            { 0x94, PhysicalKey.LaunchApp2 },
            { 0xA3, PhysicalKey.LaunchMail },
            { 0xAC, PhysicalKey.MediaPlayPause },
            { 0xB3, PhysicalKey.MediaSelect },
            { 0xAE, PhysicalKey.MediaStop },
            { 0xAB, PhysicalKey.MediaTrackNext },
            { 0xAD, PhysicalKey.MediaTrackPrevious },
            { 0x7C, PhysicalKey.Power },
            { 0x96, PhysicalKey.Sleep },
            { 0x7A, PhysicalKey.AudioVolumeDown },
            { 0x79, PhysicalKey.AudioVolumeMute },
            { 0x7B, PhysicalKey.AudioVolumeUp },
            { 0x97, PhysicalKey.WakeUp },

            // Legacy Keys
            { 0x89, PhysicalKey.Again },
            { 0x8D, PhysicalKey.Copy },
            { 0x91, PhysicalKey.Cut },
            { 0x90, PhysicalKey.Find },
            { 0x8E, PhysicalKey.Open },
            { 0x8F, PhysicalKey.Paste },
            //{     , PhysicalKey.Props },
            { 0x8C, PhysicalKey.Select },
            { 0x8B, PhysicalKey.Undo }
        };

        public static PhysicalKey PhysicalKeyFromScanCode(int scanCode)
            => scanCode is > 0 and <= 255 && s_physicalKeyFromScanCode.TryGetValue((byte)scanCode, out var result) ?
                result :
                PhysicalKey.None;

        private static readonly Dictionary<X11Key, Key> s_keyFromX11Key = new(180)
        {
            {X11Key.Cancel, Key.Cancel},
            {X11Key.BackSpace, Key.Back},
            {X11Key.Tab, Key.Tab},
            {X11Key.Linefeed, Key.LineFeed},
            {X11Key.Clear, Key.Clear},
            {X11Key.Return, Key.Return},
            {X11Key.KP_Enter, Key.Return},
            {X11Key.Pause, Key.Pause},
            {X11Key.Caps_Lock, Key.CapsLock},
            //{ X11Key.?, Key.HangulMode }
            //{ X11Key.?, Key.JunjaMode }
            //{ X11Key.?, Key.FinalMode }
            //{ X11Key.?, Key.KanjiMode }
            {X11Key.Escape, Key.Escape},
            //{ X11Key.?, Key.ImeConvert }
            //{ X11Key.?, Key.ImeNonConvert }
            //{ X11Key.?, Key.ImeAccept }
            //{ X11Key.?, Key.ImeModeChange }
            {X11Key.space, Key.Space},
            {X11Key.Prior, Key.Prior},
            {X11Key.KP_Prior, Key.Prior},
            {X11Key.Page_Down, Key.PageDown},
            {X11Key.KP_Page_Down, Key.PageDown},
            {X11Key.End, Key.End},
            {X11Key.KP_End, Key.End},
            {X11Key.Home, Key.Home},
            {X11Key.KP_Home, Key.Home},
            {X11Key.Left, Key.Left},
            {X11Key.KP_Left, Key.Left},
            {X11Key.Up, Key.Up},
            {X11Key.KP_Up, Key.Up},
            {X11Key.Right, Key.Right},
            {X11Key.KP_Right, Key.Right},
            {X11Key.Down, Key.Down},
            {X11Key.KP_Down, Key.Down},
            {X11Key.Select, Key.Select},
            {X11Key.Print, Key.Print},
            {X11Key.Execute, Key.Execute},
            //{ X11Key.?, Key.Snapshot }
            {X11Key.Insert, Key.Insert},
            {X11Key.KP_Insert, Key.Insert},
            {X11Key.Delete, Key.Delete},
            {X11Key.KP_Delete, Key.Delete},
            {X11Key.Help, Key.Help},
            {X11Key.A, Key.A},
            {X11Key.B, Key.B},
            {X11Key.C, Key.C},
            {X11Key.D, Key.D},
            {X11Key.E, Key.E},
            {X11Key.F, Key.F},
            {X11Key.G, Key.G},
            {X11Key.H, Key.H},
            {X11Key.I, Key.I},
            {X11Key.J, Key.J},
            {X11Key.K, Key.K},
            {X11Key.L, Key.L},
            {X11Key.M, Key.M},
            {X11Key.N, Key.N},
            {X11Key.O, Key.O},
            {X11Key.P, Key.P},
            {X11Key.Q, Key.Q},
            {X11Key.R, Key.R},
            {X11Key.S, Key.S},
            {X11Key.T, Key.T},
            {X11Key.U, Key.U},
            {X11Key.V, Key.V},
            {X11Key.W, Key.W},
            {X11Key.X, Key.X},
            {X11Key.Y, Key.Y},
            {X11Key.Z, Key.Z},
            {X11Key.a, Key.A},
            {X11Key.b, Key.B},
            {X11Key.c, Key.C},
            {X11Key.d, Key.D},
            {X11Key.e, Key.E},
            {X11Key.f, Key.F},
            {X11Key.g, Key.G},
            {X11Key.h, Key.H},
            {X11Key.i, Key.I},
            {X11Key.j, Key.J},
            {X11Key.k, Key.K},
            {X11Key.l, Key.L},
            {X11Key.m, Key.M},
            {X11Key.n, Key.N},
            {X11Key.o, Key.O},
            {X11Key.p, Key.P},
            {X11Key.q, Key.Q},
            {X11Key.r, Key.R},
            {X11Key.s, Key.S},
            {X11Key.t, Key.T},
            {X11Key.u, Key.U},
            {X11Key.v, Key.V},
            {X11Key.w, Key.W},
            {X11Key.x, Key.X},
            {X11Key.y, Key.Y},
            {X11Key.z, Key.Z},
            {X11Key.Super_L, Key.LWin },
            {X11Key.Super_R, Key.RWin },
            {X11Key.Menu, Key.Apps},
            //{ X11Key.?, Key.Sleep }
            {X11Key.KP_0, Key.NumPad0},
            {X11Key.KP_1, Key.NumPad1},
            {X11Key.KP_2, Key.NumPad2},
            {X11Key.KP_3, Key.NumPad3},
            {X11Key.KP_4, Key.NumPad4},
            {X11Key.KP_5, Key.NumPad5},
            {X11Key.KP_6, Key.NumPad6},
            {X11Key.KP_7, Key.NumPad7},
            {X11Key.KP_8, Key.NumPad8},
            {X11Key.KP_9, Key.NumPad9},
            {X11Key.multiply, Key.Multiply},
            {X11Key.KP_Multiply, Key.Multiply},
            {X11Key.KP_Add, Key.Add},
            //{ X11Key.?, Key.Separator }
            {X11Key.KP_Subtract, Key.Subtract},
            {X11Key.KP_Decimal, Key.Decimal},
            {X11Key.KP_Divide, Key.Divide},
            {X11Key.F1, Key.F1},
            {X11Key.F2, Key.F2},
            {X11Key.F3, Key.F3},
            {X11Key.F4, Key.F4},
            {X11Key.F5, Key.F5},
            {X11Key.F6, Key.F6},
            {X11Key.F7, Key.F7},
            {X11Key.F8, Key.F8},
            {X11Key.F9, Key.F9},
            {X11Key.F10, Key.F10},
            {X11Key.F11, Key.F11},
            {X11Key.F12, Key.F12},
            {X11Key.L3, Key.F13},
            {X11Key.F14, Key.F14},
            {X11Key.L5, Key.F15},
            {X11Key.F16, Key.F16},
            {X11Key.F17, Key.F17},
            {X11Key.L8, Key.F18},
            {X11Key.L9, Key.F19},
            {X11Key.L10, Key.F20},
            {X11Key.R1, Key.F21},
            {X11Key.R2, Key.F22},
            {X11Key.F23, Key.F23},
            {X11Key.R4, Key.F24},
            {X11Key.Num_Lock, Key.NumLock},
            {X11Key.Scroll_Lock, Key.Scroll},
            {X11Key.Shift_L, Key.LeftShift},
            {X11Key.Shift_R, Key.RightShift},
            {X11Key.Control_L, Key.LeftCtrl},
            {X11Key.Control_R, Key.RightCtrl},
            {X11Key.Alt_L, Key.LeftAlt},
            {X11Key.Alt_R, Key.RightAlt},
            //{ X11Key.?, Key.BrowserBack }
            //{ X11Key.?, Key.BrowserForward }
            //{ X11Key.?, Key.BrowserRefresh }
            //{ X11Key.?, Key.BrowserStop }
            //{ X11Key.?, Key.BrowserSearch }
            //{ X11Key.?, Key.BrowserFavorites }
            //{ X11Key.?, Key.BrowserHome }
            //{ X11Key.?, Key.VolumeMute }
            //{ X11Key.?, Key.VolumeDown }
            //{ X11Key.?, Key.VolumeUp }
            //{ X11Key.?, Key.MediaNextTrack }
            //{ X11Key.?, Key.MediaPreviousTrack }
            //{ X11Key.?, Key.MediaStop }
            //{ X11Key.?, Key.MediaPlayPause }
            //{ X11Key.?, Key.LaunchMail }
            //{ X11Key.?, Key.SelectMedia }
            //{ X11Key.?, Key.LaunchApplication1 }
            //{ X11Key.?, Key.LaunchApplication2 }
            {X11Key.minus, Key.OemMinus},
            {X11Key.underscore, Key.OemMinus},
            {X11Key.plus, Key.OemPlus},
            {X11Key.equal, Key.OemPlus},
            {X11Key.bracketleft, Key.OemOpenBrackets},
            {X11Key.braceleft, Key.OemOpenBrackets},
            {X11Key.bracketright, Key.OemCloseBrackets},
            {X11Key.braceright, Key.OemCloseBrackets},
            {X11Key.backslash, Key.OemPipe},
            {X11Key.bar, Key.OemPipe},
            {X11Key.semicolon, Key.OemSemicolon},
            {X11Key.colon, Key.OemSemicolon},
            {X11Key.apostrophe, Key.OemQuotes},
            {X11Key.quotedbl, Key.OemQuotes},
            {X11Key.comma, Key.OemComma},
            {X11Key.less, Key.OemComma},
            {X11Key.period, Key.OemPeriod},
            {X11Key.greater, Key.OemPeriod},
            {X11Key.slash, Key.Oem2},
            {X11Key.question, Key.Oem2},
            {X11Key.grave, Key.OemTilde},
            {X11Key.asciitilde, Key.OemTilde},
            {X11Key.XK_1, Key.D1},
            {X11Key.XK_2, Key.D2},
            {X11Key.XK_3, Key.D3},
            {X11Key.XK_4, Key.D4},
            {X11Key.XK_5, Key.D5},
            {X11Key.XK_6, Key.D6},
            {X11Key.XK_7, Key.D7},
            {X11Key.XK_8, Key.D8},
            {X11Key.XK_9, Key.D9},
            {X11Key.XK_0, Key.D0},
            //{ X11Key.?, Key.AbntC1 }
            //{ X11Key.?, Key.AbntC2 }
            //{ X11Key.?, Key.Oem8 }
            //{ X11Key.?, Key.Oem102 }
            //{ X11Key.?, Key.ImeProcessed }
            //{ X11Key.?, Key.System }
            //{ X11Key.?, Key.OemAttn }
            //{ X11Key.?, Key.OemFinish }
            //{ X11Key.?, Key.DbeHiragana }
            //{ X11Key.?, Key.OemAuto }
            //{ X11Key.?, Key.DbeDbcsChar }
            //{ X11Key.?, Key.OemBackTab }
            //{ X11Key.?, Key.Attn }
            //{ X11Key.?, Key.DbeEnterWordRegisterMode }
            //{ X11Key.?, Key.DbeEnterImeConfigureMode }
            //{ X11Key.?, Key.EraseEof }
            //{ X11Key.?, Key.Play }
            //{ X11Key.?, Key.Zoom }
            //{ X11Key.?, Key.NoName }
            //{ X11Key.?, Key.DbeEnterDialogConversionMode }
            //{ X11Key.?, Key.OemClear }
            //{ X11Key.?, Key.DeadCharProcessed }
        };

        public static Key KeyFromX11Key(X11Key key)
            => s_keyFromX11Key.TryGetValue(key, out var result) ? result : Key.None;
    }
    
}
