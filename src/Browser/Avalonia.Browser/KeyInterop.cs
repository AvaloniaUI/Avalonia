using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.Browser
{
    internal static class KeyInterop
    {
        // https://www.w3.org/TR/uievents-code/
        // https://developer.mozilla.org/en-US/docs/Web/API/UI_Events/Keyboard_event_code_values
        // This list has the same order as the PhysicalKey enum.
        private static readonly Dictionary<string, PhysicalKey> s_physicalKeyFromDomCode = new()
        {
            { "Unidentified", PhysicalKey.None },

            // Writing System Keys
            { "Backquote", PhysicalKey.Backquote },
            { "Backslash", PhysicalKey.Backslash },
            { "BracketLeft", PhysicalKey.BracketLeft },
            { "BracketRight", PhysicalKey.BracketRight },
            { "Comma", PhysicalKey.Comma },
            { "Digit0", PhysicalKey.Digit0 },
            { "Digit1", PhysicalKey.Digit1 },
            { "Digit2", PhysicalKey.Digit2 },
            { "Digit3", PhysicalKey.Digit3 },
            { "Digit4", PhysicalKey.Digit4 },
            { "Digit5", PhysicalKey.Digit5 },
            { "Digit6", PhysicalKey.Digit6 },
            { "Digit7", PhysicalKey.Digit7 },
            { "Digit8", PhysicalKey.Digit8 },
            { "Digit9", PhysicalKey.Digit9 },
            { "Equal", PhysicalKey.Equal },
            { "IntlBackslash", PhysicalKey.IntlBackslash },
            { "IntlRo", PhysicalKey.IntlRo },
            { "IntlYen", PhysicalKey.IntlYen },
            { "KeyA", PhysicalKey.A },
            { "KeyB", PhysicalKey.B },
            { "KeyC", PhysicalKey.C },
            { "KeyD", PhysicalKey.D },
            { "KeyE", PhysicalKey.E },
            { "KeyF", PhysicalKey.F },
            { "KeyG", PhysicalKey.G },
            { "KeyH", PhysicalKey.H },
            { "KeyI", PhysicalKey.I },
            { "KeyJ", PhysicalKey.J },
            { "KeyK", PhysicalKey.K },
            { "KeyL", PhysicalKey.L },
            { "KeyM", PhysicalKey.M },
            { "KeyN", PhysicalKey.N },
            { "KeyO", PhysicalKey.O },
            { "KeyP", PhysicalKey.P },
            { "KeyQ", PhysicalKey.Q },
            { "KeyR", PhysicalKey.R },
            { "KeyS", PhysicalKey.S },
            { "KeyT", PhysicalKey.T },
            { "KeyU", PhysicalKey.U },
            { "KeyV", PhysicalKey.V },
            { "KeyW", PhysicalKey.W },
            { "KeyX", PhysicalKey.X },
            { "KeyY", PhysicalKey.Y },
            { "KeyZ", PhysicalKey.Z },
            { "Minus", PhysicalKey.Minus },
            { "Period", PhysicalKey.Period },
            { "Quote", PhysicalKey.Quote },
            { "Semicolon", PhysicalKey.Semicolon },
            { "Slash", PhysicalKey.Slash },

            // Functional Keys
            { "AltLeft", PhysicalKey.AltLeft },
            { "AltRight", PhysicalKey.AltRight },
            { "Backspace", PhysicalKey.Backspace },
            { "CapsLock", PhysicalKey.CapsLock },
            { "ContextMenu", PhysicalKey.ContextMenu },
            { "ControlLeft", PhysicalKey.ControlLeft },
            { "ControlRight", PhysicalKey.ControlRight },
            { "Enter", PhysicalKey.Enter },
            { "MetaLeft", PhysicalKey.MetaLeft },
            { "OSLeft", PhysicalKey.MetaLeft },
            { "MetaRight", PhysicalKey.MetaRight },
            { "OSRight", PhysicalKey.MetaRight },
            { "ShiftLeft", PhysicalKey.ShiftLeft },
            { "ShiftRight", PhysicalKey.ShiftRight },
            { "Space", PhysicalKey.Space },
            { "Tab", PhysicalKey.Tab },
            { "Convert", PhysicalKey.Convert },
            { "KanaMode", PhysicalKey.KanaMode },
            { "Lang1", PhysicalKey.Lang1 },
            { "Lang2", PhysicalKey.Lang2 },
            { "Lang3", PhysicalKey.Lang3 },
            { "Lang4", PhysicalKey.Lang4 },
            { "Lang5", PhysicalKey.Lang5 },
            { "NonConvert", PhysicalKey.NonConvert },

            // Control Pad Section
            { "Delete", PhysicalKey.Delete },
            { "End", PhysicalKey.End },
            { "Help", PhysicalKey.Help },
            { "Home", PhysicalKey.Home },
            { "Insert", PhysicalKey.Insert },
            { "PageDown", PhysicalKey.PageDown },
            { "PageUp", PhysicalKey.PageUp },

            // Arrow Pad Section
            { "ArrowDown", PhysicalKey.ArrowDown },
            { "ArrowLeft", PhysicalKey.ArrowLeft },
            { "ArrowRight", PhysicalKey.ArrowRight },
            { "ArrowUp", PhysicalKey.ArrowUp },

            // Numpad Section
            { "NumLock", PhysicalKey.NumLock },
            { "Numpad0", PhysicalKey.NumPad0 },
            { "Numpad1", PhysicalKey.NumPad1 },
            { "Numpad2", PhysicalKey.NumPad2 },
            { "Numpad3", PhysicalKey.NumPad3 },
            { "Numpad4", PhysicalKey.NumPad4 },
            { "Numpad5", PhysicalKey.NumPad5 },
            { "Numpad6", PhysicalKey.NumPad6 },
            { "Numpad7", PhysicalKey.NumPad7 },
            { "Numpad8", PhysicalKey.NumPad8 },
            { "Numpad9", PhysicalKey.NumPad9 },
            { "NumpadAdd", PhysicalKey.NumPadAdd },
            { "NumpadClear", PhysicalKey.NumPadClear },
            { "NumpadComma", PhysicalKey.NumPadComma },
            { "NumpadDecimal", PhysicalKey.NumPadDecimal },
            { "NumpadDivide", PhysicalKey.NumPadDivide },
            { "NumpadEnter", PhysicalKey.NumPadEnter },
            { "NumpadEqual", PhysicalKey.NumPadEqual },
            { "NumpadMultiply", PhysicalKey.NumPadMultiply },
            { "NumpadParenLeft", PhysicalKey.NumPadParenLeft },
            { "NumpadParenRight", PhysicalKey.NumPadParenRight },
            { "NumpadSubtract", PhysicalKey.NumPadSubtract },

            // Function Section
            { "Escape", PhysicalKey.Escape },
            { "F1", PhysicalKey.F1 },
            { "F2", PhysicalKey.F2 },
            { "F3", PhysicalKey.F3 },
            { "F4", PhysicalKey.F4 },
            { "F5", PhysicalKey.F5 },
            { "F6", PhysicalKey.F6 },
            { "F7", PhysicalKey.F7 },
            { "F8", PhysicalKey.F8 },
            { "F9", PhysicalKey.F9 },
            { "F10", PhysicalKey.F10 },
            { "F11", PhysicalKey.F11 },
            { "F12", PhysicalKey.F12 },
            { "F13", PhysicalKey.F13 },
            { "F14", PhysicalKey.F14 },
            { "F15", PhysicalKey.F15 },
            { "F16", PhysicalKey.F16 },
            { "F17", PhysicalKey.F17 },
            { "F18", PhysicalKey.F18 },
            { "F19", PhysicalKey.F19 },
            { "F20", PhysicalKey.F20 },
            { "F21", PhysicalKey.F21 },
            { "F22", PhysicalKey.F22 },
            { "F23", PhysicalKey.F23 },
            { "F24", PhysicalKey.F24 },
            { "PrintScreen", PhysicalKey.PrintScreen },
            { "ScrollLock", PhysicalKey.ScrollLock },
            { "Pause", PhysicalKey.Pause },

            // Media Keys
            { "BrowserBack", PhysicalKey.BrowserBack },
            { "BrowserFavorites", PhysicalKey.BrowserFavorites },
            { "BrowserForward", PhysicalKey.BrowserForward },
            { "BrowserHome", PhysicalKey.BrowserHome },
            { "BrowserRefresh", PhysicalKey.BrowserRefresh },
            { "BrowserSearch", PhysicalKey.BrowserSearch },
            { "BrowserStop", PhysicalKey.BrowserStop },
            { "Abort", PhysicalKey.BrowserStop },
            { "Eject", PhysicalKey.Eject },
            { "LaunchApp1", PhysicalKey.LaunchApp1 },
            { "LaunchApp2", PhysicalKey.LaunchApp2 },
            { "LaunchMail", PhysicalKey.LaunchMail },
            { "MediaPlayPause", PhysicalKey.MediaPlayPause },
            { "MediaSelect", PhysicalKey.MediaSelect },
            { "MediaStop", PhysicalKey.MediaStop },
            { "MediaTrackNext", PhysicalKey.MediaTrackNext },
            { "MediaTrackPrevious", PhysicalKey.MediaTrackPrevious },
            { "Power", PhysicalKey.Power },
            { "Sleep", PhysicalKey.Sleep },
            { "AudioVolumeDown", PhysicalKey.AudioVolumeDown },
            { "VolumeDown", PhysicalKey.AudioVolumeDown },
            { "AudioVolumeMute", PhysicalKey.AudioVolumeMute },
            { "VolumeMute", PhysicalKey.AudioVolumeMute },
            { "AudioVolumeUp", PhysicalKey.AudioVolumeUp },
            { "VolumeUp", PhysicalKey.AudioVolumeUp },
            { "WakeUp", PhysicalKey.WakeUp },

            // Legacy Keys
            { "Copy", PhysicalKey.Copy },
            { "Cut", PhysicalKey.Cut },
            { "Find", PhysicalKey.Find },
            { "Open", PhysicalKey.Open },
            { "Paste", PhysicalKey.Paste },
            { "Props", PhysicalKey.Props },
            { "Select", PhysicalKey.Select },
            { "Undo", PhysicalKey.Undo }
        };

        public static PhysicalKey PhysicalKeyFromDomCode(string? domCode)
            => !string.IsNullOrEmpty(domCode) && s_physicalKeyFromDomCode.TryGetValue(domCode, out var physicalKey) ?
                physicalKey :
                PhysicalKey.None;

        // https://developer.mozilla.org/en-US/docs/Web/API/UI_Events/Keyboard_event_key_values
        private static readonly Dictionary<string, Key> s_keyFromDomKey = new()
        {
            // Alphabetic keys
            { "A", Key.A },
            { "B", Key.B },
            { "C", Key.C },
            { "D", Key.D },
            { "E", Key.E },
            { "F", Key.F },
            { "G", Key.G },
            { "H", Key.H },
            { "I", Key.I },
            { "J", Key.J },
            { "K", Key.K },
            { "L", Key.L },
            { "M", Key.M },
            { "N", Key.N },
            { "O", Key.O },
            { "P", Key.P },
            { "Q", Key.Q },
            { "R", Key.R },
            { "S", Key.S },
            { "T", Key.T },
            { "U", Key.U },
            { "V", Key.V },
            { "W", Key.W },
            { "X", Key.X },
            { "Y", Key.Y },
            { "Z", Key.Z },
            { "a", Key.A },
            { "b", Key.B },
            { "c", Key.C },
            { "d", Key.D },
            { "e", Key.E },
            { "f", Key.F },
            { "g", Key.G },
            { "h", Key.H },
            { "i", Key.I },
            { "j", Key.J },
            { "k", Key.K },
            { "l", Key.L },
            { "m", Key.M },
            { "n", Key.N },
            { "o", Key.O },
            { "p", Key.P },
            { "q", Key.Q },
            { "r", Key.R },
            { "s", Key.S },
            { "t", Key.T },
            { "u", Key.U },
            { "v", Key.V },
            { "w", Key.W },
            { "x", Key.X },
            { "y", Key.Y },
            { "z", Key.Z },

            // Modifier keys (left/right keys are handled separately)
            { "AltGr", Key.RightAlt },
            { "CapsLock", Key.CapsLock },
            { "NumLock", Key.NumLock },
            { "ScrollLock", Key.Scroll },

            // Whitespace keys
            { "Enter", Key.Enter },
            { "Tab", Key.Tab },
            { " ", Key.Space },

            // Navigation keys
            { "ArrowDown", Key.Down },
            { "ArrowLeft", Key.Left },
            { "ArrowRight", Key.Right },
            { "ArrowUp", Key.Up },
            { "End", Key.End },
            { "Home", Key.Home },
            { "PageDown", Key.PageDown },
            { "PageUp", Key.PageUp },

            // Editing keys
            { "Backspace", Key.Back },
            { "Clear", Key.Clear },
            { "CrSel", Key.CrSel },
            { "Delete", Key.Delete },
            { "EraseEof", Key.EraseEof },
            { "ExSel", Key.ExSel },
            { "Insert", Key.Insert },

            // UI keys
            { "Accept", Key.ImeAccept },
            { "Attn", Key.OemAttn },
            { "Cancel", Key.Cancel },
            { "ContextMenu", Key.Apps },
            { "Escape", Key.Escape },
            { "Execute", Key.Execute },
            { "Finish", Key.OemFinish },
            { "Help", Key.Help },
            { "Pause", Key.Pause },
            { "Play", Key.Play },
            { "Select", Key.Select },
            { "ZoomIn", Key.Zoom },

            // Device keys
            { "PrintScreen", Key.PrintScreen },

            // IME keys
            { "Convert", Key.ImeConvert },
            { "FinalMode", Key.FinalMode },
            { "ModeChange", Key.ImeModeChange },
            { "NonConvert", Key.ImeNonConvert },
            { "Process", Key.ImeProcessed },
            { "HangulMode", Key.HangulMode },
            { "HanjaMode", Key.HanjaMode },
            { "JunjaMode", Key.JunjaMode },
            { "Hankaku", Key.OemAuto },
            { "Hiragana", Key.DbeHiragana },
            { "KanaMode", Key.KanaMode },
            { "KanjiMode", Key.KanjiMode },
            { "Katakana", Key.DbeKatakana },
            { "Romaji", Key.OemBackTab },
            { "Zenkaku", Key.OemEnlw },

            // Function keys
            { "F1", Key.F1 },
            { "F2", Key.F2 },
            { "F3", Key.F3 },
            { "F4", Key.F4 },
            { "F5", Key.F5 },
            { "F6", Key.F6 },
            { "F7", Key.F7 },
            { "F8", Key.F8 },
            { "F9", Key.F9 },
            { "F10", Key.F10 },
            { "F11", Key.F11 },
            { "F12", Key.F12 },
            { "F13", Key.F13 },
            { "F14", Key.F14 },
            { "F15", Key.F15 },
            { "F16", Key.F16 },
            { "F17", Key.F17 },
            { "F18", Key.F18 },
            { "F19", Key.F19 },
            { "F20", Key.F20 },

            // Multimedia keys
            { "MediaPlayPause", Key.MediaPlayPause },
            { "MediaStop", Key.MediaStop },
            { "MediaTrackNext", Key.MediaNextTrack },
            { "MediaTrackPrevious", Key.MediaPreviousTrack },

            // Audio control keys
            { "AudioVolumeDown", Key.VolumeDown },
            { "AudioVolumeMute", Key.VolumeMute },
            { "AudioVolumeUp", Key.VolumeUp },

            // Application selector keys
            { "LaunchCalculator", Key.LaunchApplication2 },
            { "LaunchMail", Key.LaunchMail },
            { "LaunchMyComputer", Key.LaunchApplication1 },
            { "LaunchApplication1", Key.LaunchApplication1 },
            { "LaunchApplication2", Key.LaunchApplication2 },

            // Browser control keys
            { "BrowserBack", Key.BrowserBack },
            { "BrowserFavorites", Key.BrowserFavorites },
            { "BrowserForward", Key.BrowserForward },
            { "BrowserHome", Key.BrowserHome },
            { "BrowserRefresh", Key.BrowserRefresh },
            { "BrowserSearch", Key.BrowserSearch },
            { "BrowserStop", Key.BrowserStop },

            // Numeric keypad keys
            { "Decimal", Key.Decimal },
            { "Multiply", Key.Multiply },
            { "Add", Key.Add },
            { "Divide", Key.Divide },
            { "Subtract", Key.Subtract },
            { "Separator", Key.Separator },
        };

        public static Key KeyFromDomKey(string? domKey, PhysicalKey physicalKey)
        {
            if (string.IsNullOrEmpty(domKey))
                return Key.None;

            if (s_keyFromDomKey.TryGetValue(domKey, out var key))
                return key;

            key = domKey switch
            {
                "Alt" => physicalKey == PhysicalKey.AltRight ? Key.RightAlt : Key.LeftAlt,
                "Control" => physicalKey == PhysicalKey.ControlRight ? Key.RightCtrl : Key.LeftCtrl,
                "Shift" => physicalKey == PhysicalKey.ShiftRight ? Key.RightShift : Key.LeftShift,
                "Meta" => physicalKey == PhysicalKey.MetaRight ? Key.RWin : Key.LWin,
                "0" => physicalKey == PhysicalKey.NumPad0 ? Key.NumPad0 : Key.D0,
                "1" => physicalKey == PhysicalKey.NumPad1 ? Key.NumPad1 : Key.D1,
                "2" => physicalKey == PhysicalKey.NumPad2 ? Key.NumPad2 : Key.D2,
                "3" => physicalKey == PhysicalKey.NumPad3 ? Key.NumPad3 : Key.D3,
                "4" => physicalKey == PhysicalKey.NumPad4 ? Key.NumPad4 : Key.D4,
                "5" => physicalKey == PhysicalKey.NumPad5 ? Key.NumPad5 : Key.D5,
                "6" => physicalKey == PhysicalKey.NumPad6 ? Key.NumPad6 : Key.D6,
                "7" => physicalKey == PhysicalKey.NumPad7 ? Key.NumPad7 : Key.D7,
                "8" => physicalKey == PhysicalKey.NumPad8 ? Key.NumPad8 : Key.D8,
                "9" => physicalKey == PhysicalKey.NumPad9 ? Key.NumPad9 : Key.D9,
                "+" => physicalKey == PhysicalKey.NumPadAdd ? Key.Add : Key.OemPlus,
                "-" => physicalKey == PhysicalKey.NumPadSubtract ? Key.Subtract : Key.OemMinus,
                "*" => physicalKey == PhysicalKey.NumPadMultiply ? Key.Multiply : Key.None,
                "/" => physicalKey == PhysicalKey.NumPadDivide ? Key.Divide : Key.None,
                _ => Key.None
            };

            if (key != Key.None)
                return key;

            return physicalKey.ToQwertyKey();
        }

        public static string? KeySymbolFromDomKey(string? domKey)
        {
            if (string.IsNullOrEmpty(domKey))
                return null;

            return domKey.Length switch
            {
                1 => domKey,
                2 when char.IsSurrogatePair(domKey[0], domKey[1]) => domKey,
                _ => null
            };
        }
    }
}
