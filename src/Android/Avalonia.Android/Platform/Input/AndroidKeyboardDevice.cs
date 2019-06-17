//using Android.InputMethodServices;
using System.Collections.Generic;
using Android.Views;
using Avalonia.Input;

namespace Avalonia.Android.Platform.Input
{
    public class AndroidKeyboardDevice : KeyboardDevice, IKeyboardDevice {
        public static readonly KeyboardDevice Instance = new AndroidKeyboardDevice();
    private static readonly Dictionary<Keycode, Key> KeyDic = new Dictionary<Keycode, Key>
     {
         //   { Keycode.Cancel?, Key.Cancel },
            { Keycode.Del, Key.Back },
            { Keycode.Tab, Key.Tab },
          //  { Keycode.Linefeed?, Key.LineFeed },
            { Keycode.Clear, Key.Clear },
            { Keycode.Enter, Key.Return },
            { Keycode.MediaPause, Key.Pause },
            //{ Keycode.?, Key.CapsLock }
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
           // { Keycode.end?, Key.End },
            { Keycode.Home, Key.Home },
            { Keycode.DpadLeft, Key.Left },
            { Keycode.DpadUp, Key.Up },
            { Keycode.DpadRight, Key.Right },
            { Keycode.DpadDown, Key.Down },
           // { Keycode.ButtonSelect?, Key.Select },
           // { Keycode.print?, Key.Print },
            //{ Keycode.execute?, Key.Execute },
           // { Keycode.snap, Key.Snapshot }
            { Keycode.Insert, Key.Insert },
            { Keycode.ForwardDel, Key.Delete },
            //{ Keycode.help, Key.Help },
            //{ Keycode.?, Key.D0 }
            //{ Keycode.?, Key.D1 }
            //{ Keycode.?, Key.D2 }
            //{ Keycode.?, Key.D3 }
            //{ Keycode.?, Key.D4 }
            //{ Keycode.?, Key.D5 }
            //{ Keycode.?, Key.D6 }
            //{ Keycode.?, Key.D7 }
            //{ Keycode.?, Key.D8 }
            //{ Keycode.?, Key.D9 }
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
            //{ Keycode.?, Key.Sleep }
            //{ Keycode.?, Key.NumPad0 }
            //{ Keycode.?, Key.NumPad1 }
            //{ Keycode.?, Key.NumPad2 }
            //{ Keycode.?, Key.NumPad3 }
            //{ Keycode.?, Key.NumPad4 }
            //{ Keycode.?, Key.NumPad5 }
            //{ Keycode.?, Key.NumPad6 }
            //{ Keycode.?, Key.NumPad7 }
            //{ Keycode.?, Key.NumPad8 }
            //{ Keycode.?, Key.NumPad9 }
            { Keycode.NumpadMultiply, Key.Multiply },
            { Keycode.NumpadAdd, Key.Add },
            { Keycode.NumpadComma, Key.Separator },
            { Keycode.NumpadSubtract, Key.Subtract },
            //{ Keycode.numpaddecimal?, Key.Decimal }
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
           // { Keycode.numpad, Key.NumLock }
            { Keycode.ScrollLock, Key.Scroll },
            { Keycode.ShiftLeft, Key.LeftShift },
            //{ Keycode.?, Key.RightShift }
            //{ Keycode.?, Key.LeftCtrl }
            //{ Keycode.?, Key.RightCtrl }
            //{ Keycode.?, Key.LeftAlt }
            //{ Keycode.?, Key.RightAlt }
            //{ Keycode.?, Key.BrowserBack }
            //{ Keycode.?, Key.BrowserForward }
            //{ Keycode.?, Key.BrowserRefresh }
            //{ Keycode.?, Key.BrowserStop }
            //{ Keycode.?, Key.BrowserSearch }
            //{ Keycode.?, Key.BrowserFavorites }
            //{ Keycode.?, Key.BrowserHome }
            //{ Keycode.?, Key.VolumeMute }
            //{ Keycode.?, Key.VolumeDown }
            //{ Keycode.?, Key.VolumeUp }
            //{ Keycode.?, Key.MediaNextTrack }
            //{ Keycode.?, Key.MediaPreviousTrack }
            //{ Keycode.?, Key.MediaStop }
            //{ Keycode.?, Key.MediaPlayPause }
            //{ Keycode.?, Key.LaunchMail }
            //{ Keycode.?, Key.SelectMedia }
            //{ Keycode.?, Key.LaunchApplication1 }
            //{ Keycode.?, Key.LaunchApplication2 }
            //{ Keycode.?, Key.OemSemicolon }
            //{ Keycode.?, Key.OemPlus }
            //{ Keycode.?, Key.OemComma }
            //{ Keycode.?, Key.OemMinus }
            //{ Keycode.?, Key.OemPeriod }
            //{ Keycode.?, Key.Oem2 }
            //{ Keycode.?, Key.OemTilde }
            //{ Keycode.?, Key.AbntC1 }
            //{ Keycode.?, Key.AbntC2 }
            //{ Keycode.?, Key.Oem4 }
            //{ Keycode.?, Key.OemPipe }
            //{ Keycode.?, Key.OemCloseBrackets }
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
            //{ Keycode.?, Key.Play }
            //{ Keycode.?, Key.Zoom }
            //{ Keycode.?, Key.NoName }
            //{ Keycode.?, Key.DbeEnterDialogConversionMode }
            //{ Keycode.?, Key.OemClear }
            //{ Keycode.?, Key.DeadCharProcessed }
        };

    internal static Key ConvertKey(Keycode key) {
      Key result;
      return KeyDic.TryGetValue(key, out result) ? result : Key.None;
    }
  }
}
