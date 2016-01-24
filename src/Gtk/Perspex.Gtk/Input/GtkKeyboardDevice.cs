// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Perspex.Input;

namespace Perspex.Gtk
{
    public class GtkKeyboardDevice : KeyboardDevice
    {
        private static readonly Dictionary<Gdk.Key, Key> KeyDic = new Dictionary<Gdk.Key, Key>
        {
            { Gdk.Key.Cancel, Key.Cancel },
            { Gdk.Key.BackSpace, Key.Back },
            { Gdk.Key.Tab, Key.Tab },
            { Gdk.Key.Linefeed, Key.LineFeed },
            { Gdk.Key.Clear, Key.Clear },
            { Gdk.Key.Return, Key.Return },
            { Gdk.Key.Pause, Key.Pause },
            //{ Gdk.Key.?, Key.CapsLock }
            //{ Gdk.Key.?, Key.HangulMode }
            //{ Gdk.Key.?, Key.JunjaMode }
            //{ Gdk.Key.?, Key.FinalMode }
            //{ Gdk.Key.?, Key.KanjiMode }
            { Gdk.Key.Escape, Key.Escape },
            //{ Gdk.Key.?, Key.ImeConvert }
            //{ Gdk.Key.?, Key.ImeNonConvert }
            //{ Gdk.Key.?, Key.ImeAccept }
            //{ Gdk.Key.?, Key.ImeModeChange }
            { Gdk.Key.space, Key.Space },
            { Gdk.Key.Prior, Key.Prior },
            //{ Gdk.Key.?, Key.PageDown }
            { Gdk.Key.End, Key.End },
            { Gdk.Key.Home, Key.Home },
            { Gdk.Key.Left, Key.Left },
            { Gdk.Key.Up, Key.Up },
            { Gdk.Key.Right, Key.Right },
            { Gdk.Key.Down, Key.Down },
            { Gdk.Key.Select, Key.Select },
            { Gdk.Key.Print, Key.Print },
            { Gdk.Key.Execute, Key.Execute },
            //{ Gdk.Key.?, Key.Snapshot }
            { Gdk.Key.Insert, Key.Insert },
            { Gdk.Key.Delete, Key.Delete },
            { Gdk.Key.Help, Key.Help },
            //{ Gdk.Key.?, Key.D0 }
            //{ Gdk.Key.?, Key.D1 }
            //{ Gdk.Key.?, Key.D2 }
            //{ Gdk.Key.?, Key.D3 }
            //{ Gdk.Key.?, Key.D4 }
            //{ Gdk.Key.?, Key.D5 }
            //{ Gdk.Key.?, Key.D6 }
            //{ Gdk.Key.?, Key.D7 }
            //{ Gdk.Key.?, Key.D8 }
            //{ Gdk.Key.?, Key.D9 }
            { Gdk.Key.A, Key.A },
            { Gdk.Key.B, Key.B },
            { Gdk.Key.C, Key.C },
            { Gdk.Key.D, Key.D },
            { Gdk.Key.E, Key.E },
            { Gdk.Key.F, Key.F },
            { Gdk.Key.G, Key.G },
            { Gdk.Key.H, Key.H },
            { Gdk.Key.I, Key.I },
            { Gdk.Key.J, Key.J },
            { Gdk.Key.K, Key.K },
            { Gdk.Key.L, Key.L },
            { Gdk.Key.M, Key.M },
            { Gdk.Key.N, Key.N },
            { Gdk.Key.O, Key.O },
            { Gdk.Key.P, Key.P },
            { Gdk.Key.Q, Key.Q },
            { Gdk.Key.R, Key.R },
            { Gdk.Key.S, Key.S },
            { Gdk.Key.T, Key.T },
            { Gdk.Key.U, Key.U },
            { Gdk.Key.V, Key.V },
            { Gdk.Key.W, Key.W },
            { Gdk.Key.X, Key.X },
            { Gdk.Key.Y, Key.Y },
            { Gdk.Key.Z, Key.Z },
            { Gdk.Key.a, Key.A },
            { Gdk.Key.b, Key.B },
            { Gdk.Key.c, Key.C },
            { Gdk.Key.d, Key.D },
            { Gdk.Key.e, Key.E },
            { Gdk.Key.f, Key.F },
            { Gdk.Key.g, Key.G },
            { Gdk.Key.h, Key.H },
            { Gdk.Key.i, Key.I },
            { Gdk.Key.j, Key.J },
            { Gdk.Key.k, Key.K },
            { Gdk.Key.l, Key.L },
            { Gdk.Key.m, Key.M },
            { Gdk.Key.n, Key.N },
            { Gdk.Key.o, Key.O },
            { Gdk.Key.p, Key.P },
            { Gdk.Key.q, Key.Q },
            { Gdk.Key.r, Key.R },
            { Gdk.Key.s, Key.S },
            { Gdk.Key.t, Key.T },
            { Gdk.Key.u, Key.U },
            { Gdk.Key.v, Key.V },
            { Gdk.Key.w, Key.W },
            { Gdk.Key.x, Key.X },
            { Gdk.Key.y, Key.Y },
            { Gdk.Key.z, Key.Z },
            //{ Gdk.Key.?, Key.LWin }
            //{ Gdk.Key.?, Key.RWin }
            //{ Gdk.Key.?, Key.Apps }
            //{ Gdk.Key.?, Key.Sleep }
            //{ Gdk.Key.?, Key.NumPad0 }
            //{ Gdk.Key.?, Key.NumPad1 }
            //{ Gdk.Key.?, Key.NumPad2 }
            //{ Gdk.Key.?, Key.NumPad3 }
            //{ Gdk.Key.?, Key.NumPad4 }
            //{ Gdk.Key.?, Key.NumPad5 }
            //{ Gdk.Key.?, Key.NumPad6 }
            //{ Gdk.Key.?, Key.NumPad7 }
            //{ Gdk.Key.?, Key.NumPad8 }
            //{ Gdk.Key.?, Key.NumPad9 }
            { Gdk.Key.multiply, Key.Multiply },
            //{ Gdk.Key.?, Key.Add }
            //{ Gdk.Key.?, Key.Separator }
            //{ Gdk.Key.?, Key.Subtract }
            //{ Gdk.Key.?, Key.Decimal }
            //{ Gdk.Key.?, Key.Divide }
            { Gdk.Key.F1, Key.F1 },
            { Gdk.Key.F2, Key.F2 },
            { Gdk.Key.F3, Key.F3 },
            { Gdk.Key.F4, Key.F4 },
            { Gdk.Key.F5, Key.F5 },
            { Gdk.Key.F6, Key.F6 },
            { Gdk.Key.F7, Key.F7 },
            { Gdk.Key.F8, Key.F8 },
            { Gdk.Key.F9, Key.F9 },
            { Gdk.Key.F10, Key.F10 },
            { Gdk.Key.F11, Key.F11 },
            { Gdk.Key.F12, Key.F12 },
            { Gdk.Key.L3, Key.F13 },
            { Gdk.Key.F14, Key.F14 },
            { Gdk.Key.L5, Key.F15 },
            { Gdk.Key.F16, Key.F16 },
            { Gdk.Key.F17, Key.F17 },
            { Gdk.Key.L8, Key.F18 },
            { Gdk.Key.L9, Key.F19 },
            { Gdk.Key.L10, Key.F20 },
            { Gdk.Key.R1, Key.F21 },
            { Gdk.Key.R2, Key.F22 },
            { Gdk.Key.F23, Key.F23 },
            { Gdk.Key.R4, Key.F24 },
            //{ Gdk.Key.?, Key.NumLock }
            //{ Gdk.Key.?, Key.Scroll }
            //{ Gdk.Key.?, Key.LeftShift }
            //{ Gdk.Key.?, Key.RightShift }
            //{ Gdk.Key.?, Key.LeftCtrl }
            //{ Gdk.Key.?, Key.RightCtrl }
            //{ Gdk.Key.?, Key.LeftAlt }
            //{ Gdk.Key.?, Key.RightAlt }
            //{ Gdk.Key.?, Key.BrowserBack }
            //{ Gdk.Key.?, Key.BrowserForward }
            //{ Gdk.Key.?, Key.BrowserRefresh }
            //{ Gdk.Key.?, Key.BrowserStop }
            //{ Gdk.Key.?, Key.BrowserSearch }
            //{ Gdk.Key.?, Key.BrowserFavorites }
            //{ Gdk.Key.?, Key.BrowserHome }
            //{ Gdk.Key.?, Key.VolumeMute }
            //{ Gdk.Key.?, Key.VolumeDown }
            //{ Gdk.Key.?, Key.VolumeUp }
            //{ Gdk.Key.?, Key.MediaNextTrack }
            //{ Gdk.Key.?, Key.MediaPreviousTrack }
            //{ Gdk.Key.?, Key.MediaStop }
            //{ Gdk.Key.?, Key.MediaPlayPause }
            //{ Gdk.Key.?, Key.LaunchMail }
            //{ Gdk.Key.?, Key.SelectMedia }
            //{ Gdk.Key.?, Key.LaunchApplication1 }
            //{ Gdk.Key.?, Key.LaunchApplication2 }
            //{ Gdk.Key.?, Key.OemSemicolon }
            //{ Gdk.Key.?, Key.OemPlus }
            //{ Gdk.Key.?, Key.OemComma }
            //{ Gdk.Key.?, Key.OemMinus }
            //{ Gdk.Key.?, Key.OemPeriod }
            //{ Gdk.Key.?, Key.Oem2 }
            //{ Gdk.Key.?, Key.OemTilde }
            //{ Gdk.Key.?, Key.AbntC1 }
            //{ Gdk.Key.?, Key.AbntC2 }
            //{ Gdk.Key.?, Key.Oem4 }
            //{ Gdk.Key.?, Key.OemPipe }
            //{ Gdk.Key.?, Key.OemCloseBrackets }
            //{ Gdk.Key.?, Key.Oem7 }
            //{ Gdk.Key.?, Key.Oem8 }
            //{ Gdk.Key.?, Key.Oem102 }
            //{ Gdk.Key.?, Key.ImeProcessed }
            //{ Gdk.Key.?, Key.System }
            //{ Gdk.Key.?, Key.OemAttn }
            //{ Gdk.Key.?, Key.OemFinish }
            //{ Gdk.Key.?, Key.DbeHiragana }
            //{ Gdk.Key.?, Key.OemAuto }
            //{ Gdk.Key.?, Key.DbeDbcsChar }
            //{ Gdk.Key.?, Key.OemBackTab }
            //{ Gdk.Key.?, Key.Attn }
            //{ Gdk.Key.?, Key.DbeEnterWordRegisterMode }
            //{ Gdk.Key.?, Key.DbeEnterImeConfigureMode }
            //{ Gdk.Key.?, Key.EraseEof }
            //{ Gdk.Key.?, Key.Play }
            //{ Gdk.Key.?, Key.Zoom }
            //{ Gdk.Key.?, Key.NoName }
            //{ Gdk.Key.?, Key.DbeEnterDialogConversionMode }
            //{ Gdk.Key.?, Key.OemClear }
            //{ Gdk.Key.?, Key.DeadCharProcessed }
        };

        public new static GtkKeyboardDevice Instance { get; } = new GtkKeyboardDevice();

        public static Key ConvertKey(Gdk.Key key)
        {
            Key result;
            return KeyDic.TryGetValue(key, out result) ? result : Key.None;
        }
    }
}