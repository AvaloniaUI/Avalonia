using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Tizen.Uix.InputMethod;

namespace Avalonia.Tizen.Platform.Input;
internal class TizenKeyboardDevice: KeyboardDevice, IKeyboardDevice
{
    private static readonly Dictionary<KeyCode, Key> KeyDic = new Dictionary<KeyCode, Key>
     {
         //   { KeyCode.Cancel?, Key.Cancel },
            { KeyCode.BackSpace, Key.Back },
            { KeyCode.Tab, Key.Tab },
          //  { KeyCode.Linefeed?, Key.LineFeed },
            { KeyCode.Clear, Key.Clear },
            { KeyCode.Return, Key.Return },
            { KeyCode.Pause, Key.Pause },
            { KeyCode.CapsLock, Key.CapsLock },
            //{ KeyCode.?, Key.HangulMode }
            //{ KeyCode.?, Key.JunjaMode }
            //{ KeyCode.?, Key.FinalMode }
            //{ KeyCode.?, Key.KanjiMode }
            { KeyCode.Escape, Key.Escape },
            //{ KeyCode.?, Key.ImeConvert }
            //{ KeyCode.?, Key.ImeNonConvert }
            //{ KeyCode.?, Key.ImeAccept }
            //{ KeyCode.?, Key.ImeModeChange }
            { KeyCode.Space, Key.Space },
            { KeyCode.Page_Up, Key.Prior },
            { KeyCode.Page_Down, Key.PageDown },
            { KeyCode.End, Key.End },
            { KeyCode.Home, Key.Home },
            { KeyCode.Left, Key.Left },
            { KeyCode.Up, Key.Up },
            { KeyCode.Right, Key.Right },
            { KeyCode.Down, Key.Down },
           // { KeyCode.ButtonSelect?, Key.Select },
           // { KeyCode.print?, Key.Print },
            //{ KeyCode.execute?, Key.Execute },
            //{ KeyCode.snap?, Key.Snapshot }
            { KeyCode.Insert, Key.Insert },
            { KeyCode.Delete, Key.Delete },
            { KeyCode.Help, Key.Help },
            { KeyCode.Keypad0, Key.D0 },
            { KeyCode.Keypad1, Key.D1 },
            { KeyCode.Keypad2, Key.D2 },
            { KeyCode.Keypad3, Key.D3 },
            { KeyCode.Keypad4, Key.D4 },
            { KeyCode.Keypad5, Key.D5 },
            { KeyCode.Keypad6, Key.D6 },
            { KeyCode.Keypad7, Key.D7 },
            { KeyCode.Keypad8, Key.D8 },
            { KeyCode.Keypad9, Key.D9 },
            { KeyCode.KeypadA, Key.A },
            { KeyCode.KeypadB, Key.B },
            { KeyCode.KeypadC, Key.C },
            { KeyCode.KeypadD, Key.D },
            { KeyCode.KeypadE, Key.E },
            { KeyCode.KeypadF, Key.F },
            { KeyCode.KeypadG, Key.G },
            { KeyCode.KeypadH, Key.H },
            { KeyCode.KeypadI, Key.I },
            { KeyCode.KeypadJ, Key.J },
            { KeyCode.KeypadK, Key.K },
            { KeyCode.KeypadL, Key.L },
            { KeyCode.KeypadM, Key.M },
            { KeyCode.KeypadN, Key.N },
            { KeyCode.KeypadO, Key.O },
            { KeyCode.KeypadP, Key.P },
            { KeyCode.KeypadQ, Key.Q },
            { KeyCode.KeypadR, Key.R },
            { KeyCode.KeypadS, Key.S },
            { KeyCode.KeypadT, Key.T },
            { KeyCode.KeypadU, Key.U },
            { KeyCode.KeypadV, Key.V },
            { KeyCode.KeypadW, Key.W },
            { KeyCode.KeypadX, Key.X },
            { KeyCode.KeypadY, Key.Y },
            { KeyCode.KeypadZ, Key.Z },
            { KeyCode.Keypada, Key.A },
            { KeyCode.Keypadb, Key.B },
            { KeyCode.Keypadc, Key.C },
            { KeyCode.Keypadd, Key.D },
            { KeyCode.Keypade, Key.E },
            { KeyCode.Keypadf, Key.F },
            { KeyCode.Keypadg, Key.G },
            { KeyCode.Keypadh, Key.H },
            { KeyCode.Keypadi, Key.I },
            { KeyCode.Keypadj, Key.J },
            { KeyCode.Keypadk, Key.K },
            { KeyCode.Keypadl, Key.L },
            { KeyCode.Keypadm, Key.M },
            { KeyCode.Keypadn, Key.N },
            { KeyCode.Keypado, Key.O },
            { KeyCode.Keypadp, Key.P },
            { KeyCode.Keypadq, Key.Q },
            { KeyCode.Keypadr, Key.R },
            { KeyCode.Keypads, Key.S },
            { KeyCode.Keypadt, Key.T },
            { KeyCode.Keypadu, Key.U },
            { KeyCode.Keypadv, Key.V },
            { KeyCode.Keypadw, Key.W },
            { KeyCode.Keypadx, Key.X },
            { KeyCode.Keypady, Key.Y },
            { KeyCode.Keypadz, Key.Z },
            //{ KeyCode.?, Key.LWin }
            //{ KeyCode.?, Key.RWin }
            //{ KeyCode.?, Key.Apps }
            //{ KeyCode.Sle, Key.Sleep },
            { KeyCode.KP0, Key.NumPad0 },
            { KeyCode.KP1, Key.NumPad1 },
            { KeyCode.KP2, Key.NumPad2 },
            { KeyCode.KP3, Key.NumPad3 },
            { KeyCode.KP4, Key.NumPad4 },
            { KeyCode.KP5, Key.NumPad5 },
            { KeyCode.KP6, Key.NumPad6 },
            { KeyCode.KP7, Key.NumPad7 },
            { KeyCode.KP8, Key.NumPad8 },
            { KeyCode.KP9, Key.NumPad9 },
            { KeyCode.KPMultiply, Key.Multiply },
            { KeyCode.KPAdd, Key.Add },
            { KeyCode.KPSeparator, Key.Separator },
            { KeyCode.KPSubtract, Key.Subtract },
            { KeyCode.KPDecimal, Key.Decimal },
            { KeyCode.KPDivide, Key.Divide },
            { KeyCode.F1, Key.F1 },
            { KeyCode.F2, Key.F2 },
            { KeyCode.F3, Key.F3 },
            { KeyCode.F4, Key.F4 },
            { KeyCode.F5, Key.F5 },
            { KeyCode.F6, Key.F6 },
            { KeyCode.F7, Key.F7 },
            { KeyCode.F8, Key.F8 },
            { KeyCode.F9, Key.F9 },
            { KeyCode.F10, Key.F10 },
            { KeyCode.F11, Key.F11 },
            { KeyCode.F12, Key.F12 },
            //{ KeyCode.f13, Key.F13 },
            //{ KeyCode.F14, Key.F14 },
            //{ KeyCode.L5, Key.F15 },
            //{ KeyCode.F16, Key.F16 },
            //{ KeyCode.F17, Key.F17 },
            //{ KeyCode.L8, Key.F18 },
            //{ KeyCode.L9, Key.F19 },
            //{ KeyCode.L10, Key.F20 },
            //{ KeyCode.R1, Key.F21 },
            //{ KeyCode.R2, Key.F22 },
            //{ KeyCode.F23, Key.F23 },
            //{ KeyCode.R4, Key.F24 },
            { KeyCode.Num_Lock, Key.NumLock },
            { KeyCode.ScrollLock, Key.Scroll },
            { KeyCode.ShiftL, Key.LeftShift },
            { KeyCode.ShiftR, Key.RightShift },
            { KeyCode.ControlL, Key.LeftCtrl },
            { KeyCode.ControlR, Key.RightCtrl },
            { KeyCode.AltL, Key.LeftAlt },
            { KeyCode.AltR, Key.RightAlt },
            //{ KeyCode.?, Key.BrowserBack }
            //{ KeyCode.?, Key.BrowserForward }
            //{ KeyCode.?, Key.BrowserRefresh }
            //{ KeyCode.?, Key.BrowserStop }
            //{ KeyCode.?, Key.BrowserSearch }
            //{ KeyCode.?, Key.BrowserFavorites }
            //{ KeyCode.?, Key.BrowserHome }
            //{ KeyCode.?, Key.VolumeMute }
            //{ KeyCode.VolumeDown, Key.VolumeDown },
            //{ KeyCode.VolumeUp, Key.VolumeUp },
            //{ KeyCode.MediaNext, Key.MediaNextTrack },
            //{ KeyCode.MediaPrevious, Key.MediaPreviousTrack },
            //{ KeyCode.MediaStop, Key.MediaStop },
            //{ KeyCode.MediaPlayPause, Key.MediaPlayPause },
            //{ KeyCode.?, Key.LaunchMail }
            //{ KeyCode.?, Key.SelectMedia }
            //{ KeyCode.?, Key.LaunchApplication1 }
            //{ KeyCode.?, Key.LaunchApplication2 }
            { KeyCode.Semicolon, Key.OemSemicolon },
            { KeyCode.Plus, Key.OemPlus },
            { KeyCode.Comma, Key.OemComma },
            { KeyCode.Minus, Key.OemMinus },
            { KeyCode.Period, Key.OemPeriod },
            //{ KeyCode.?, Key.Oem2 }
            { KeyCode.Grave, Key.OemTilde },
            //{ KeyCode.?, Key.AbntC1 }
            //{ KeyCode.?, Key.AbntC2 }
            //{ KeyCode.?, Key.OemPipe }
            { KeyCode.Apostrophe, Key.OemQuotes },
            { KeyCode.Slash, Key.OemQuestion },
            { KeyCode.BraceLeft, Key.OemOpenBrackets },
            { KeyCode.BracketRight, Key.OemCloseBrackets },
            //{ KeyCode.?, Key.Oem7 }
            //{ KeyCode.?, Key.Oem8 }
            //{ KeyCode.?, Key.Oem102 }
            //{ KeyCode.?, Key.ImeProcessed }
            //{ KeyCode.?, Key.System }
            //{ KeyCode.?, Key.OemAttn }
            //{ KeyCode.?, Key.OemFinish }
            //{ KeyCode.?, Key.DbeHiragana }
            //{ KeyCode.?, Key.OemAuto }
            //{ KeyCode.?, Key.DbeDbcsChar }
            //{ KeyCode.?, Key.OemBackTab }
            //{ KeyCode.?, Key.Attn }
            //{ KeyCode.?, Key.DbeEnterWordRegisterMode }
            //{ KeyCode.?, Key.DbeEnterImeConfigureMode }
            //{ KeyCode.?, Key.EraseEof }
            //{ KeyCode.MediaPlay, Key.Play },
            //{ KeyCode.?, Key.Zoom }
            //{ KeyCode.?, Key.NoName }
            //{ KeyCode.?, Key.DbeEnterDialogConversionMode }
            //{ KeyCode.?, Key.OemClear }
            //{ KeyCode.?, Key.DeadCharProcessed }
            { KeyCode.Backslash, Key.OemBackslash }
        };

    internal static Key ConvertKey(KeyCode key)
    {
        return KeyDic.TryGetValue(key, out var result) ? result : Key.None;
    }
}
