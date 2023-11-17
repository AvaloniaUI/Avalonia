using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using static Avalonia.Win32.Interop.UnmanagedMethods.VirtualKeyStates;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// Contains methods used to translate a Windows virtual/physical key to an Avalonia <see cref="Key"/>.
    /// </summary>
    public static class KeyInterop
    {
        // source: https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        private static readonly Dictionary<Key, int> s_virtualKeyFromKey = new(169)
        {
            { Key.Cancel, (int)VK_CANCEL },
            { Key.Back, (int)VK_BACK },
            { Key.Tab, (int)VK_TAB },
            { Key.Clear, (int)VK_CLEAR },
            { Key.Return, (int)VK_RETURN },
            { Key.Pause, (int)VK_PAUSE },
            { Key.Capital, (int)VK_CAPITAL },
            { Key.KanaMode, (int)VK_KANA },
            { Key.JunjaMode, (int)VK_JUNJA },
            { Key.FinalMode, (int)VK_FINAL },
            { Key.HanjaMode, (int)VK_HANJA },
            { Key.Escape, (int)VK_ESCAPE },
            { Key.ImeConvert, (int)VK_CONVERT },
            { Key.ImeNonConvert, (int)VK_NONCONVERT },
            { Key.ImeAccept, (int)VK_ACCEPT },
            { Key.ImeModeChange, (int)VK_MODECHANGE },
            { Key.Space, (int)VK_SPACE },
            { Key.PageUp, (int)VK_PRIOR },
            { Key.PageDown, (int)VK_NEXT },
            { Key.End, (int)VK_END },
            { Key.Home, (int)VK_HOME },
            { Key.Left, (int)VK_LEFT },
            { Key.Up, (int)VK_UP },
            { Key.Right, (int)VK_RIGHT },
            { Key.Down, (int)VK_DOWN },
            { Key.Select, (int)VK_SELECT },
            { Key.Print, (int)VK_PRINT },
            { Key.Execute, (int)VK_EXECUTE },
            { Key.Snapshot, (int)VK_SNAPSHOT },
            { Key.Insert, (int)VK_INSERT },
            { Key.Delete, (int)VK_DELETE },
            { Key.Help, (int)VK_HELP },
            { Key.D0, '0' },
            { Key.D1, '1' },
            { Key.D2, '2' },
            { Key.D3, '3' },
            { Key.D4, '4' },
            { Key.D5, '5' },
            { Key.D6, '6' },
            { Key.D7, '7' },
            { Key.D8, '8' },
            { Key.D9, '9' },
            { Key.A, 'A' },
            { Key.B, 'B' },
            { Key.C, 'C' },
            { Key.D, 'D' },
            { Key.E, 'E' },
            { Key.F, 'F' },
            { Key.G, 'G' },
            { Key.H, 'H' },
            { Key.I, 'I' },
            { Key.J, 'J' },
            { Key.K, 'K' },
            { Key.L, 'L' },
            { Key.M, 'M' },
            { Key.N, 'N' },
            { Key.O, 'O' },
            { Key.P, 'P' },
            { Key.Q, 'Q' },
            { Key.R, 'R' },
            { Key.S, 'S' },
            { Key.T, 'T' },
            { Key.U, 'U' },
            { Key.V, 'V' },
            { Key.W, 'W' },
            { Key.X, 'X' },
            { Key.Y, 'Y' },
            { Key.Z, 'Z' },
            { Key.LWin, (int)VK_LWIN },
            { Key.RWin, (int)VK_RWIN },
            { Key.Apps, (int)VK_APPS },
            { Key.Sleep, (int)VK_SLEEP },
            { Key.NumPad0, (int)VK_NUMPAD0 },
            { Key.NumPad1, (int)VK_NUMPAD1 },
            { Key.NumPad2, (int)VK_NUMPAD2 },
            { Key.NumPad3, (int)VK_NUMPAD3 },
            { Key.NumPad4, (int)VK_NUMPAD4 },
            { Key.NumPad5, (int)VK_NUMPAD5 },
            { Key.NumPad6, (int)VK_NUMPAD6 },
            { Key.NumPad7, (int)VK_NUMPAD7 },
            { Key.NumPad8, (int)VK_NUMPAD8 },
            { Key.NumPad9, (int)VK_NUMPAD9 },
            { Key.Multiply, (int)VK_MULTIPLY },
            { Key.Add, (int)VK_ADD },
            { Key.Separator, (int)VK_SEPARATOR },
            { Key.Subtract, (int)VK_SUBTRACT },
            { Key.Decimal, (int)VK_DECIMAL },
            { Key.Divide, (int)VK_DIVIDE },
            { Key.F1, (int)VK_F1 },
            { Key.F2, (int)VK_F2 },
            { Key.F3, (int)VK_F3 },
            { Key.F4, (int)VK_F4 },
            { Key.F5, (int)VK_F5 },
            { Key.F6, (int)VK_F6 },
            { Key.F7, (int)VK_F7 },
            { Key.F8, (int)VK_F8 },
            { Key.F9, (int)VK_F9 },
            { Key.F10, (int)VK_F10 },
            { Key.F11, (int)VK_F11 },
            { Key.F12, (int)VK_F12 },
            { Key.F13, (int)VK_F13 },
            { Key.F14, (int)VK_F14 },
            { Key.F15, (int)VK_F15 },
            { Key.F16, (int)VK_F16 },
            { Key.F17, (int)VK_F17 },
            { Key.F18, (int)VK_F18 },
            { Key.F19, (int)VK_F19 },
            { Key.F20, (int)VK_F20 },
            { Key.F21, (int)VK_F21 },
            { Key.F22, (int)VK_F22 },
            { Key.F23, (int)VK_F23 },
            { Key.F24, (int)VK_F24 },
            { Key.NumLock, (int)VK_NUMLOCK },
            { Key.Scroll, (int)VK_SCROLL },
            { Key.LeftShift, (int)VK_LSHIFT },
            { Key.RightShift, (int)VK_RSHIFT },
            { Key.LeftCtrl, (int)VK_LCONTROL },
            { Key.RightCtrl, (int)VK_RCONTROL },
            { Key.LeftAlt, (int)VK_LMENU },
            { Key.RightAlt, (int)VK_RMENU },
            { Key.BrowserBack, (int)VK_BROWSER_BACK },
            { Key.BrowserForward, (int)VK_BROWSER_FORWARD },
            { Key.BrowserRefresh, (int)VK_BROWSER_REFRESH },
            { Key.BrowserStop, (int)VK_BROWSER_STOP },
            { Key.BrowserSearch, (int)VK_BROWSER_SEARCH },
            { Key.BrowserFavorites, (int)VK_BROWSER_FAVORITES },
            { Key.BrowserHome, (int)VK_BROWSER_HOME },
            { Key.VolumeMute, (int)VK_VOLUME_MUTE },
            { Key.VolumeDown, (int)VK_VOLUME_DOWN },
            { Key.VolumeUp, (int)VK_VOLUME_UP },
            { Key.MediaNextTrack, (int)VK_MEDIA_NEXT_TRACK },
            { Key.MediaPreviousTrack, (int)VK_MEDIA_PREV_TRACK },
            { Key.MediaStop, (int)VK_MEDIA_STOP },
            { Key.MediaPlayPause, (int)VK_MEDIA_PLAY_PAUSE },
            { Key.LaunchMail, (int)VK_LAUNCH_MAIL },
            { Key.SelectMedia, (int)VK_LAUNCH_MEDIA_SELECT },
            { Key.LaunchApplication1, (int)VK_LAUNCH_APP1 },
            { Key.LaunchApplication2, (int)VK_LAUNCH_APP2 },
            { Key.Oem1, (int)VK_OEM_1 },
            { Key.OemPlus, (int)VK_OEM_PLUS },
            { Key.OemComma, (int)VK_OEM_COMMA },
            { Key.OemMinus, (int)VK_OEM_MINUS },
            { Key.OemPeriod, (int)VK_OEM_PERIOD },
            { Key.OemQuestion, (int)VK_OEM_2 },
            { Key.Oem3, (int)VK_OEM_3 },
            { Key.AbntC1, (int)VK_ABNT_C1 },
            { Key.AbntC2, (int)VK_ABNT_C2 },
            { Key.OemOpenBrackets, (int)VK_OEM_4 },
            { Key.Oem5, (int)VK_OEM_5 },
            { Key.Oem6, (int)VK_OEM_6 },
            { Key.OemQuotes, (int)VK_OEM_7 },
            { Key.Oem8, (int)VK_OEM_8 },
            { Key.OemBackslash, (int)VK_OEM_102 },
            { Key.ImeProcessed, (int)VK_PROCESSKEY },
            { Key.OemAttn, (int)VK_OEM_ATTN },
            { Key.OemFinish, (int)VK_OEM_FINISH },
            { Key.OemCopy, (int)VK_OEM_COPY },
            { Key.DbeSbcsChar, (int)VK_OEM_AUTO },
            { Key.OemEnlw, (int)VK_OEM_ENLW },
            { Key.OemBackTab, (int)VK_OEM_BACKTAB },
            { Key.DbeNoRoman, (int)VK_ATTN },
            { Key.DbeEnterWordRegisterMode, (int)VK_CRSEL },
            { Key.DbeEnterImeConfigureMode, (int)VK_EXSEL },
            { Key.EraseEof, (int)VK_EREOF },
            { Key.Play, (int)VK_PLAY },
            { Key.DbeNoCodeInput, (int)VK_ZOOM },
            { Key.NoName, (int)VK_NONAME },
            { Key.Pa1, (int)VK_PA1 },
            { Key.OemClear, (int)VK_OEM_CLEAR }
        };

        private static readonly Dictionary<int, Key> s_keyFromVirtualKey =
            s_virtualKeyFromKey.ToDictionary(pair => pair.Value, pair => pair.Key);

        // https://learn.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input#scan-codes
        // https://github.com/chromium/chromium/blob/main/ui/events/keycodes/dom/dom_code_data.inc
        // This list has the same order as the PhysicalKey enum.
        private static readonly Dictionary<ushort, PhysicalKey> s_physicalKeyFromExtendedScanCode = new(155)
        {
            // Writing System Keys
            { 0x0029, PhysicalKey.Backquote },
            { 0x002B, PhysicalKey.Backslash },
            { 0x001A, PhysicalKey.BracketLeft },
            { 0x001B, PhysicalKey.BracketRight },
            { 0x0033, PhysicalKey.Comma },
            { 0x000B, PhysicalKey.Digit0 },
            { 0x0002, PhysicalKey.Digit1 },
            { 0x0003, PhysicalKey.Digit2 },
            { 0x0004, PhysicalKey.Digit3 },
            { 0x0005, PhysicalKey.Digit4 },
            { 0x0006, PhysicalKey.Digit5 },
            { 0x0007, PhysicalKey.Digit6 },
            { 0x0008, PhysicalKey.Digit7 },
            { 0x0009, PhysicalKey.Digit8 },
            { 0x000A, PhysicalKey.Digit9 },
            { 0x000D, PhysicalKey.Equal },
            { 0x0056, PhysicalKey.IntlBackslash },
            { 0x0073, PhysicalKey.IntlRo },
            { 0x007D, PhysicalKey.IntlYen },
            { 0x001E, PhysicalKey.A },
            { 0x0030, PhysicalKey.B },
            { 0x002E, PhysicalKey.C },
            { 0x0020, PhysicalKey.D },
            { 0x0012, PhysicalKey.E },
            { 0x0021, PhysicalKey.F },
            { 0x0022, PhysicalKey.G },
            { 0x0023, PhysicalKey.H },
            { 0x0017, PhysicalKey.I },
            { 0x0024, PhysicalKey.J },
            { 0x0025, PhysicalKey.K },
            { 0x0026, PhysicalKey.L },
            { 0x0032, PhysicalKey.M },
            { 0x0031, PhysicalKey.N },
            { 0x0018, PhysicalKey.O },
            { 0x0019, PhysicalKey.P },
            { 0x0010, PhysicalKey.Q },
            { 0x0013, PhysicalKey.R },
            { 0x001F, PhysicalKey.S },
            { 0x0014, PhysicalKey.T },
            { 0x0016, PhysicalKey.U },
            { 0x002F, PhysicalKey.V },
            { 0x0011, PhysicalKey.W },
            { 0x002D, PhysicalKey.X },
            { 0x0015, PhysicalKey.Y },
            { 0x002C, PhysicalKey.Z },
            { 0x000C, PhysicalKey.Minus },
            { 0x0034, PhysicalKey.Period },
            { 0x0028, PhysicalKey.Quote },
            { 0x0027, PhysicalKey.Semicolon },
            { 0x0035, PhysicalKey.Slash },

            // Functional Keys
            { 0x0038, PhysicalKey.AltLeft },
            { 0xE038, PhysicalKey.AltRight },
            { 0x000E, PhysicalKey.Backspace },
            { 0x003A, PhysicalKey.CapsLock },
            { 0xE05D, PhysicalKey.ContextMenu },
            { 0x001D, PhysicalKey.ControlLeft },
            { 0xE01D, PhysicalKey.ControlRight },
            { 0x001C, PhysicalKey.Enter },
            { 0xE05B, PhysicalKey.MetaLeft },
            { 0xE05C, PhysicalKey.MetaRight },
            { 0x002A, PhysicalKey.ShiftLeft },
            { 0x0036, PhysicalKey.ShiftRight },
            { 0x0039, PhysicalKey.Space },
            { 0x000F, PhysicalKey.Tab },
            { 0x0079, PhysicalKey.Convert },
            { 0x0070, PhysicalKey.KanaMode },
            { 0x0072, PhysicalKey.Lang1 },
            { 0x0071, PhysicalKey.Lang2 },
            { 0x0078, PhysicalKey.Lang3 },
            { 0x0077, PhysicalKey.Lang4 },
            //{     , PhysicalKey.Lang5 }, Not mapped on Windows since it's the same as F24 (see Chromium remarks)
            { 0x007B, PhysicalKey.NonConvert },

            // Control Pad Section
            { 0xE053, PhysicalKey.Delete },
            { 0xE04F, PhysicalKey.End },
            { 0xE03B, PhysicalKey.Help },
            { 0xE047, PhysicalKey.Home },
            { 0xE052, PhysicalKey.Insert },
            { 0xE051, PhysicalKey.PageDown },
            { 0xE049, PhysicalKey.PageUp },

            // Arrow Pad Section
            { 0xE050, PhysicalKey.ArrowDown },
            { 0xE04B, PhysicalKey.ArrowLeft },
            { 0xE04D, PhysicalKey.ArrowRight },
            { 0xE048, PhysicalKey.ArrowUp },

            // Numpad Section
            { 0xE045, PhysicalKey.NumLock },
            { 0x0052, PhysicalKey.NumPad0 },
            { 0x004F, PhysicalKey.NumPad1 },
            { 0x0050, PhysicalKey.NumPad2 },
            { 0x0051, PhysicalKey.NumPad3 },
            { 0x004B, PhysicalKey.NumPad4 },
            { 0x004C, PhysicalKey.NumPad5 },
            { 0x004D, PhysicalKey.NumPad6 },
            { 0x0047, PhysicalKey.NumPad7 },
            { 0x0048, PhysicalKey.NumPad8 },
            { 0x0049, PhysicalKey.NumPad9 },
            { 0x004E, PhysicalKey.NumPadAdd },
            //{     , PhysicalKey.NumPadClear },
            { 0x007E, PhysicalKey.NumPadComma },
            { 0x0053, PhysicalKey.NumPadDecimal },
            { 0xE035, PhysicalKey.NumPadDivide },
            { 0xE01C, PhysicalKey.NumPadEnter },
            { 0x0059, PhysicalKey.NumPadEqual },
            { 0x0037, PhysicalKey.NumPadMultiply },
            //{     , PhysicalKey.NumPadParenLeft },
            //{     , PhysicalKey.NumPadParenRight },
            { 0x004A, PhysicalKey.NumPadSubtract },

            // Function Section
            { 0x0001, PhysicalKey.Escape },
            { 0x003B, PhysicalKey.F1 },
            { 0x003C, PhysicalKey.F2 },
            { 0x003D, PhysicalKey.F3 },
            { 0x003E, PhysicalKey.F4 },
            { 0x003F, PhysicalKey.F5 },
            { 0x0040, PhysicalKey.F6 },
            { 0x0041, PhysicalKey.F7 },
            { 0x0042, PhysicalKey.F8 },
            { 0x0043, PhysicalKey.F9 },
            { 0x0044, PhysicalKey.F10 },
            { 0x0057, PhysicalKey.F11 },
            { 0x0058, PhysicalKey.F12 },
            { 0x0064, PhysicalKey.F13 },
            { 0x0065, PhysicalKey.F14 },
            { 0x0066, PhysicalKey.F15 },
            { 0x0067, PhysicalKey.F16 },
            { 0x0068, PhysicalKey.F17 },
            { 0x0069, PhysicalKey.F18 },
            { 0x006A, PhysicalKey.F19 },
            { 0x006B, PhysicalKey.F20 },
            { 0x006C, PhysicalKey.F21 },
            { 0x006D, PhysicalKey.F22 },
            { 0x006E, PhysicalKey.F23 },
            { 0x0076, PhysicalKey.F24 },
            { 0xE037, PhysicalKey.PrintScreen },
            { 0x0046, PhysicalKey.ScrollLock },
            { 0x0045, PhysicalKey.Pause },

            // Media Keys
            { 0xE06A, PhysicalKey.BrowserBack },
            { 0xE066, PhysicalKey.BrowserFavorites },
            { 0xE069, PhysicalKey.BrowserForward },
            { 0xE032, PhysicalKey.BrowserHome },
            { 0xE067, PhysicalKey.BrowserRefresh },
            { 0xE065, PhysicalKey.BrowserSearch },
            { 0xE068, PhysicalKey.BrowserStop },
            { 0xE02C, PhysicalKey.Eject },
            { 0xE06B, PhysicalKey.LaunchApp1 },
            { 0xE021, PhysicalKey.LaunchApp2 },
            { 0xE06C, PhysicalKey.LaunchMail },
            { 0xE022, PhysicalKey.MediaPlayPause },
            { 0xE06D, PhysicalKey.MediaSelect },
            { 0xE024, PhysicalKey.MediaStop },
            { 0xE019, PhysicalKey.MediaTrackNext },
            { 0xE010, PhysicalKey.MediaTrackPrevious },
            { 0xE05E, PhysicalKey.Power },
            { 0xE05F, PhysicalKey.Sleep },
            { 0xE02E, PhysicalKey.AudioVolumeDown },
            { 0xE020, PhysicalKey.AudioVolumeMute },
            { 0xE030, PhysicalKey.AudioVolumeUp },
            { 0xE063, PhysicalKey.WakeUp },

            // Legacy Keys
            { 0xE018, PhysicalKey.Copy },
            { 0xE017, PhysicalKey.Cut },
            //{     , PhysicalKey.Find },
            //{     , PhysicalKey.Open },
            { 0xE00A, PhysicalKey.Paste },
            //{     , PhysicalKey.Props },
            //{     , PhysicalKey.Select },
            { 0xE008, PhysicalKey.Undo },
        };

        /// <summary>
        /// Indicates whether the key is an extended key, such as the right-hand ALT and CTRL keys.
        /// According to https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown.
        /// </summary>
        private static bool IsExtended(int keyData)
        {
            const int extendedMask = 1 << 24;

            return (keyData & extendedMask) != 0;
        }

        private static byte GetScanCode(int keyData)
        {
            // Bits from 16 to 23 represent scan code.
            const int scanCodeMask = 0xFF0000;

            return (byte)((keyData & scanCodeMask) >> 16);
        }

        private static int GetVirtualKey(int virtualKey, int keyData)
        {
            // Adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndKeyboardInputProvider.cs.

            if (virtualKey == (int)VK_SHIFT)
            {
                var scanCode = GetScanCode(keyData);

                virtualKey = (int)MapVirtualKey(scanCode, (uint)MapVirtualKeyMapTypes.MAPVK_VSC_TO_VK_EX);

                if (virtualKey == 0)
                {
                    virtualKey = (int)VK_LSHIFT;
                }
            }

            else if (virtualKey == (int)VK_MENU)
            {
                bool isRight = IsExtended(keyData);

                if (isRight)
                {
                    virtualKey = (int)VK_RMENU;
                }
                else
                {
                    virtualKey = (int)VK_LMENU;
                }
            }
            
            else if (virtualKey == (int)VK_CONTROL)
            {
                bool isRight = IsExtended(keyData);

                if (isRight)
                {
                    virtualKey = (int)VK_RCONTROL;
                }
                else
                {
                    virtualKey = (int)VK_LCONTROL;
                }
            }

            return virtualKey;
        }

        /// <summary>
        /// Gets an Avalonia key from a Windows virtual-key and key data.
        /// </summary>
        /// <param name="virtualKey">The Windows virtual-key.</param>
        /// <param name="keyData">The key data (in the same format as lParam for WM_KEYDOWN).</param>
        /// <returns>An Avalonia key, or <see cref="Key.None"/> if none matched.</returns>
        public static Key KeyFromVirtualKey(int virtualKey, int keyData)
        {
            virtualKey = GetVirtualKey(virtualKey, keyData);

            s_keyFromVirtualKey.TryGetValue(virtualKey, out var result);

            return result;
        }

        /// <summary>
        /// Gets a Windows virtual-key from an Avalonia key.
        /// </summary>
        /// <param name="key">The Avalonia key.</param>
        /// <returns>A Windows virtual-key code, or 0 if none matched.</returns>
        public static int VirtualKeyFromKey(Key key)
        {
            s_virtualKeyFromKey.TryGetValue(key, out var result);

            return result;
        }

        /// <summary>
        /// Gets a physical Avalonia key from a Windows virtual-key and key data.
        /// </summary>
        /// <param name="virtualKey">The Windows virtual-key.</param>
        /// <param name="keyData">The key data (in the same format as lParam for WM_KEYDOWN).</param>
        /// <returns>An Avalonia physical key, or <see cref="PhysicalKey.None"/> if none matched.</returns>
        public static PhysicalKey PhysicalKeyFromVirtualKey(int virtualKey, int keyData)
        {
            uint scanCode = GetScanCode(keyData);
            if (scanCode == 0U)
            {
                // in some cases, the scan code contained in the keyData might be zero:
                // try to get one from the virtual key instead
                scanCode = MapVirtualKey((uint)virtualKey, (uint)MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);
                if (scanCode == 0U)
                    return PhysicalKey.None;
            }

            if (IsExtended(keyData))
                scanCode |= 0xE000;

            return scanCode is > 0 and <= 0xE0FF
                && s_physicalKeyFromExtendedScanCode.TryGetValue((ushort)scanCode, out var result) ?
                    result :
                    PhysicalKey.None;
        }

        /// <summary>
        /// Gets a key symbol from a Windows virtual-key and key data.
        /// </summary>
        /// <param name="virtualKey">The Windows virtual-key.</param>
        /// <param name="keyData">The key data (in the same format as lParam for WM_KEYDOWN).</param>
        /// <returns>A key symbol, or null if none matched.</returns>
        public static unsafe string? GetKeySymbol(int virtualKey, int keyData)
        {
            const int bufferSize = 4;
            const uint doNotChangeKeyboardState = 1U << 2;

            fixed (byte* keyStates = stackalloc byte[256])
            fixed (char* buffer = stackalloc char[bufferSize])
            {
                GetKeyboardState(keyStates);

                var length = ToUnicodeEx(
                    (uint)virtualKey,
                    GetScanCode(keyData),
                    keyStates,
                    buffer,
                    bufferSize,
                    doNotChangeKeyboardState,
                    GetKeyboardLayout(0));

                return length switch
                {
                    < 0 => new string(buffer, 0, -length), // dead key
                    0 => null,
                    1 when !KeySymbolHelper.IsAllowedAsciiKeySymbol(buffer[0]) => null,
                    2 when buffer[0] == buffer[1] => new string(buffer, 0, 1), // dead key second press repeats symbol
                    _ => new string(buffer, 0, length)
                };
            }
        }
    }
}
