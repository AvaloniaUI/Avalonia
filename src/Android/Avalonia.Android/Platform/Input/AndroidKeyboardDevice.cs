//using Android.InputMethodServices;
using System.Collections.Generic;
using Android.Views;
using Avalonia.Input;

namespace Avalonia.Android.Platform.Input
{
    internal class AndroidKeyboardDevice : KeyboardDevice, IKeyboardDevice
    {
        private static readonly Dictionary<Keycode, Key> KeyDic = new Dictionary<Keycode, Key>
     {
         //   { Keycode.Cancel?, Key.Cancel },
            { Keycode.Del, Key.Back },
            { Keycode.Tab, Key.Tab },
          //  { Keycode.Linefeed?, Key.LineFeed },
            { Keycode.Clear, Key.Clear },
            { Keycode.Enter, Key.Return },
            { Keycode.MediaPause, Key.Pause },
            { Keycode.CapsLock, Key.CapsLock },
            //{ Keycode.?, Key.HangulMode }
            //{ Keycode.?, Key.JunjaMode }
            //{ Keycode.?, Key.FinalMode }
            //{ Keycode.?, Key.KanjiMode }
            { Keycode.Escape, Key.Escape },
            //{ Keycode.?, Key.ImeConvert }
            //{ Keycode.?, Key.ImeNonConvert }
            //{ Keycode.?, Key.ImeAccept }
            //{ Keycode.?, Key.ImeModeChange }
            { Keycode.Space, Key.Space },
            { Keycode.PageUp, Key.Prior },
            { Keycode.PageDown, Key.PageDown },
            { Keycode.MoveEnd, Key.End },
            { Keycode.MoveHome, Key.Home },
           // { Keycode.ButtonSelect?, Key.Select },
           // { Keycode.print?, Key.Print },
            //{ Keycode.execute?, Key.Execute },
            //{ Keycode.snap?, Key.Snapshot }
            { Keycode.Insert, Key.Insert },
            { Keycode.ForwardDel, Key.Delete },
            { Keycode.Help, Key.Help },
            { Keycode.Num0, Key.D0 },
            { Keycode.Num1, Key.D1 },
            { Keycode.Num2, Key.D2 },
            { Keycode.Num3, Key.D3 },
            { Keycode.Num4, Key.D4 },
            { Keycode.Num5, Key.D5 },
            { Keycode.Num6, Key.D6 },
            { Keycode.Num7, Key.D7 },
            { Keycode.Num8, Key.D8 },
            { Keycode.Num9, Key.D9 },
            { Keycode.A, Key.A },
            { Keycode.B, Key.B },
            { Keycode.C, Key.C },
            { Keycode.D, Key.D },
            { Keycode.E, Key.E },
            { Keycode.F, Key.F },
            { Keycode.G, Key.G },
            { Keycode.H, Key.H },
            { Keycode.I, Key.I },
            { Keycode.J, Key.J },
            { Keycode.K, Key.K },
            { Keycode.L, Key.L },
            { Keycode.M, Key.M },
            { Keycode.N, Key.N },
            { Keycode.O, Key.O },
            { Keycode.P, Key.P },
            { Keycode.Q, Key.Q },
            { Keycode.R, Key.R },
            { Keycode.S, Key.S },
            { Keycode.T, Key.T },
            { Keycode.U, Key.U },
            { Keycode.V, Key.V },
            { Keycode.W, Key.W },
            { Keycode.X, Key.X },
            { Keycode.Y, Key.Y },
            { Keycode.Z, Key.Z },
            //{ Keycode.a, Key.A },
            //{ Keycode.b, Key.B },
            //{ Keycode.c, Key.C },
            //{ Keycode.d, Key.D },
            //{ Keycode.e, Key.E },
            //{ Keycode.f, Key.F },
            //{ Keycode.g, Key.G },
            //{ Keycode.h, Key.H },
            //{ Keycode.i, Key.I },
            //{ Keycode.j, Key.J },
            //{ Keycode.k, Key.K },
            //{ Keycode.l, Key.L },
            //{ Keycode.m, Key.M },
            //{ Keycode.n, Key.N },
            //{ Keycode.o, Key.O },
            //{ Keycode.p, Key.P },
            //{ Keycode.q, Key.Q },
            //{ Keycode.r, Key.R },
            //{ Keycode.s, Key.S },
            //{ Keycode.t, Key.T },
            //{ Keycode.u, Key.U },
            //{ Keycode.v, Key.V },
            //{ Keycode.w, Key.W },
            //{ Keycode.x, Key.X },
            //{ Keycode.y, Key.Y },
            //{ Keycode.z, Key.Z },
            //{ Keycode.?, Key.LWin }
            //{ Keycode.?, Key.RWin }
            //{ Keycode.?, Key.Apps }
            { Keycode.Sleep, Key.Sleep },
            { Keycode.Numpad0, Key.NumPad0 },
            { Keycode.Numpad1, Key.NumPad1 },
            { Keycode.Numpad2, Key.NumPad2 },
            { Keycode.Numpad3, Key.NumPad3 },
            { Keycode.Numpad4, Key.NumPad4 },
            { Keycode.Numpad5, Key.NumPad5 },
            { Keycode.Numpad6, Key.NumPad6 },
            { Keycode.Numpad7, Key.NumPad7 },
            { Keycode.Numpad8, Key.NumPad8 },
            { Keycode.Numpad9, Key.NumPad9 },
            { Keycode.NumpadMultiply, Key.Multiply },
            { Keycode.NumpadAdd, Key.Add },
            { Keycode.NumpadComma, Key.Separator },
            { Keycode.NumpadSubtract, Key.Subtract },
            { Keycode.NumpadDot, Key.Decimal },
            { Keycode.NumpadDivide, Key.Divide },
            { Keycode.F1, Key.F1 },
            { Keycode.F2, Key.F2 },
            { Keycode.F3, Key.F3 },
            { Keycode.F4, Key.F4 },
            { Keycode.F5, Key.F5 },
            { Keycode.F6, Key.F6 },
            { Keycode.F7, Key.F7 },
            { Keycode.F8, Key.F8 },
            { Keycode.F9, Key.F9 },
            { Keycode.F10, Key.F10 },
            { Keycode.F11, Key.F11 },
            { Keycode.F12, Key.F12 },
            //{ Keycode.f13, Key.F13 },
            //{ Keycode.F14, Key.F14 },
            //{ Keycode.L5, Key.F15 },
            //{ Keycode.F16, Key.F16 },
            //{ Keycode.F17, Key.F17 },
            //{ Keycode.L8, Key.F18 },
            //{ Keycode.L9, Key.F19 },
            //{ Keycode.L10, Key.F20 },
            //{ Keycode.R1, Key.F21 },
            //{ Keycode.R2, Key.F22 },
            //{ Keycode.F23, Key.F23 },
            //{ Keycode.R4, Key.F24 },
            { Keycode.NumLock, Key.NumLock },
            { Keycode.ScrollLock, Key.Scroll },
            { Keycode.ShiftLeft, Key.LeftShift },
            { Keycode.ShiftRight, Key.RightShift },
            { Keycode.CtrlLeft, Key.LeftCtrl },
            { Keycode.CtrlRight, Key.RightCtrl },
            { Keycode.AltLeft, Key.LeftAlt },
            { Keycode.AltRight, Key.RightAlt },
            //{ Keycode.?, Key.BrowserBack }
            //{ Keycode.?, Key.BrowserForward }
            //{ Keycode.?, Key.BrowserRefresh }
            //{ Keycode.?, Key.BrowserStop }
            //{ Keycode.?, Key.BrowserSearch }
            //{ Keycode.?, Key.BrowserFavorites }
            //{ Keycode.?, Key.BrowserHome }
            //{ Keycode.?, Key.VolumeMute }
            { Keycode.VolumeDown, Key.VolumeDown },
            { Keycode.VolumeUp, Key.VolumeUp },
            { Keycode.MediaNext, Key.MediaNextTrack },
            { Keycode.MediaPrevious, Key.MediaPreviousTrack },
            { Keycode.MediaStop, Key.MediaStop },
            { Keycode.MediaPlayPause, Key.MediaPlayPause },
            //{ Keycode.?, Key.LaunchMail }
            //{ Keycode.?, Key.SelectMedia }
            //{ Keycode.?, Key.LaunchApplication1 }
            //{ Keycode.?, Key.LaunchApplication2 }
            { Keycode.Semicolon, Key.OemSemicolon },
            { Keycode.Plus, Key.OemPlus },
            { Keycode.Comma, Key.OemComma },
            { Keycode.Minus, Key.OemMinus },
            { Keycode.Period, Key.OemPeriod },
            //{ Keycode.?, Key.Oem2 }
            { Keycode.Grave, Key.OemTilde },
            //{ Keycode.?, Key.AbntC1 }
            //{ Keycode.?, Key.AbntC2 }
            //{ Keycode.?, Key.OemPipe }
            { Keycode.Apostrophe, Key.OemQuotes },
            { Keycode.Slash, Key.OemQuestion },
            { Keycode.LeftBracket, Key.OemOpenBrackets },
            { Keycode.RightBracket, Key.OemCloseBrackets },
            //{ Keycode.?, Key.Oem7 }
            //{ Keycode.?, Key.Oem8 }
            //{ Keycode.?, Key.Oem102 }
            //{ Keycode.?, Key.ImeProcessed }
            //{ Keycode.?, Key.System }
            //{ Keycode.?, Key.OemAttn }
            //{ Keycode.?, Key.OemFinish }
            //{ Keycode.?, Key.DbeHiragana }
            //{ Keycode.?, Key.OemAuto }
            //{ Keycode.?, Key.DbeDbcsChar }
            //{ Keycode.?, Key.OemBackTab }
            //{ Keycode.?, Key.Attn }
            //{ Keycode.?, Key.DbeEnterWordRegisterMode }
            //{ Keycode.?, Key.DbeEnterImeConfigureMode }
            //{ Keycode.?, Key.EraseEof }
            { Keycode.MediaPlay, Key.Play },
            //{ Keycode.?, Key.Zoom }
            //{ Keycode.?, Key.NoName }
            //{ Keycode.?, Key.DbeEnterDialogConversionMode }
            //{ Keycode.?, Key.OemClear }
            //{ Keycode.?, Key.DeadCharProcessed }
            { Keycode.Backslash, Key.OemBackslash },

            // Loosely mapping DPad keys to Avalonia keys
            { Keycode.Back, Key.Escape },
            { Keycode.DpadCenter, Key.Space },
            { Keycode.DpadLeft, Key.Left },
            { Keycode.DpadUp, Key.Up },
            { Keycode.DpadRight, Key.Right },
            { Keycode.DpadDown, Key.Down }
        };

        internal static Key ConvertKey(Keycode key)
        {
            return KeyDic.TryGetValue(key, out var result) ? result : Key.None;
        }
    }
}
