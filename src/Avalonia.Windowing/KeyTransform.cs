using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.Windowing
{
    public enum VirtualKeyCode : UInt32
    {
        /// The '1' key over the letters.
        Key1,
        /// The '2' key over the letters.
        Key2,
        /// The '3' key over the letters.
        Key3,
        /// The '4' key over the letters.
        Key4,
        /// The '5' key over the letters.
        Key5,
        /// The '6' key over the letters.
        Key6,
        /// The '7' key over the letters.
        Key7,
        /// The '8' key over the letters.
        Key8,
        /// The '9' key over the letters.
        Key9,
        /// The '0' key over the 'O' and 'P' keys.
        Key0,

        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,

        /// The Escape key, next to F1.
        Escape,

        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,
        F13,
        F14,
        F15,

        /// Print Screen/SysRq.
        Snapshot,
        /// Scroll Lock.
        Scroll,
        /// Pause/Break key, next to Scroll lock.
        Pause,

        /// `Insert`, next to Backspace.
        Insert,
        Home,
        Delete,
        End,
        PageDown,
        PageUp,

        Left,
        Up,
        Right,
        Down,

        /// The Backspace key, right over Enter.
        // TODO: rename
        Back,
        /// The Enter key.
        Return,
        /// The space bar.
        Space,

        /// The "Compose" key on Linux.
        Compose,

        Caret,

        Numlock,
        Numpad0,
        Numpad1,
        Numpad2,
        Numpad3,
        Numpad4,
        Numpad5,
        Numpad6,
        Numpad7,
        Numpad8,
        Numpad9,

        AbntC1,
        AbntC2,
        Add,
        Apostrophe,
        Apps,
        At,
        Ax,
        Backslash,
        Calculator,
        Capital,
        Colon,
        Comma,
        Convert,
        Decimal,
        Divide,
        Equals,
        Grave,
        Kana,
        Kanji,
        LAlt,
        LBracket,
        LControl,
        LShift,
        LWin,
        Mail,
        MediaSelect,
        MediaStop,
        Minus,
        Multiply,
        Mute,
        MyComputer,
        NavigateForward, // also called "Prior"
        NavigateBackward, // also called "Next"
        NextTrack,
        NoConvert,
        NumpadComma,
        NumpadEnter,
        NumpadEquals,
        OEM102,
        Period,
        PlayPause,
        Power,
        PrevTrack,
        RAlt,
        RBracket,
        RControl,
        RShift,
        RWin,
        Semicolon,
        Slash,
        Sleep,
        Stop,
        Subtract,
        Sysrq,
        Tab,
        Underline,
        Unlabeled,
        VolumeDown,
        VolumeUp,
        Wake,
        WebBack,
        WebFavorites,
        WebForward,
        WebHome,
        WebRefresh,
        WebSearch,
        WebStop,
        Yen,
        Copy,
        Paste,
        Cut,
    }

    public static class KeyTransform
    {
        static readonly Dictionary<VirtualKeyCode, Key> Keys = new Dictionary<VirtualKeyCode, Key>
        {
            [VirtualKeyCode.Key0] = Key.D0,
            [VirtualKeyCode.Key1] = Key.D1,
            [VirtualKeyCode.Key2] = Key.D2,
            [VirtualKeyCode.Key3] = Key.D3,
            [VirtualKeyCode.Key4] = Key.D4,
            [VirtualKeyCode.Key5] = Key.D5,
            [VirtualKeyCode.Key6] = Key.D6,
            [VirtualKeyCode.Key7] = Key.D7,
            [VirtualKeyCode.Key8] = Key.D8,
            [VirtualKeyCode.Key9] = Key.D9,

            [VirtualKeyCode.A] = Key.A,
            [VirtualKeyCode.B] = Key.B,
            [VirtualKeyCode.C] = Key.C,
            [VirtualKeyCode.D] = Key.D,
            [VirtualKeyCode.E] = Key.E,
            [VirtualKeyCode.F] = Key.F,
            [VirtualKeyCode.G] = Key.G,
            [VirtualKeyCode.H] = Key.H,
            [VirtualKeyCode.I] = Key.I,
            [VirtualKeyCode.J] = Key.J,
            [VirtualKeyCode.K] = Key.K,
            [VirtualKeyCode.L] = Key.L,
            [VirtualKeyCode.M] = Key.M,
            [VirtualKeyCode.N] = Key.N,
            [VirtualKeyCode.O] = Key.O,
            [VirtualKeyCode.P] = Key.P,
            [VirtualKeyCode.Q] = Key.Q,
            [VirtualKeyCode.R] = Key.R,
            [VirtualKeyCode.S] = Key.S,
            [VirtualKeyCode.T] = Key.T,
            [VirtualKeyCode.U] = Key.U,
            [VirtualKeyCode.V] = Key.V,
            [VirtualKeyCode.W] = Key.W,
            [VirtualKeyCode.X] = Key.X,
            [VirtualKeyCode.Y] = Key.Y,
            [VirtualKeyCode.Z] = Key.Z,

            [VirtualKeyCode.Escape] = Key.Escape,

            [VirtualKeyCode.F1] = Key.F1,
            [VirtualKeyCode.F2] = Key.F2,
            [VirtualKeyCode.F3] = Key.F3,
            [VirtualKeyCode.F4] = Key.F4,
            [VirtualKeyCode.F5] = Key.F5,
            [VirtualKeyCode.F6] = Key.F6,
            [VirtualKeyCode.F7] = Key.F7,
            [VirtualKeyCode.F8] = Key.F8,
            [VirtualKeyCode.F9] = Key.F9,
            [VirtualKeyCode.F10] = Key.F10,
            [VirtualKeyCode.F11] = Key.F11,
            [VirtualKeyCode.F12] = Key.F12,
            [VirtualKeyCode.F13] = Key.F13,
            [VirtualKeyCode.F14] = Key.F14,
            [VirtualKeyCode.F15] = Key.F15,

            /// Print Screen/SysRq.
            [VirtualKeyCode.Snapshot] = Key.Snapshot,
            /// Scroll Lock.
            [VirtualKeyCode.Scroll] = Key.Scroll,
            /// Pause/Break key, next to Scroll lock.
            [VirtualKeyCode.Pause] = Key.Pause,

            /// `Insert`, next to Backspace.
            [VirtualKeyCode.Insert] = Key.Insert,
            [VirtualKeyCode.Home] = Key.Home,
            [VirtualKeyCode.Delete] = Key.Delete,
             [VirtualKeyCode.End] = Key.End,
              [VirtualKeyCode.PageDown] = Key.PageDown,
            [VirtualKeyCode.PageUp] = Key.PageUp,

            [VirtualKeyCode.Left] = Key.Left,
            [VirtualKeyCode.Up] = Key.Up,
            [VirtualKeyCode.Right] = Key.Right,
            [VirtualKeyCode.Down] = Key.Down,

            /// The Backspace key, right over Enter.
            // TODO: rename
            [VirtualKeyCode.Back] = Key.Back,
            /// The Enter key.
            [VirtualKeyCode.Return] = Key.Return,
            /// The space bar.
            [VirtualKeyCode.Space] = Key.Space,

            /// The "Compose" key on Linux.
            //[VirtualKeyCode.Compose] = Key.Compose, ???

            //[VirtualKeyCode.Caret] = Key.Caret,

            [VirtualKeyCode.Numlock] = Key.NumLock,
            [VirtualKeyCode.Numpad0] = Key.NumPad0,
            [VirtualKeyCode.Numpad1] = Key.NumPad1,
            [VirtualKeyCode.Numpad2] = Key.NumPad2,
            [VirtualKeyCode.Numpad3] = Key.NumPad3,
            [VirtualKeyCode.Numpad4] = Key.NumPad4,
            [VirtualKeyCode.Numpad5] = Key.NumPad5,
            [VirtualKeyCode.Numpad6] = Key.NumPad6,
            [VirtualKeyCode.Numpad7] = Key.NumPad7,
            [VirtualKeyCode.Numpad8] = Key.NumPad8,
            [VirtualKeyCode.Numpad9] = Key.NumPad9,

            [VirtualKeyCode.AbntC1] = Key.AbntC1,
            [VirtualKeyCode.AbntC2] = Key.AbntC2,
            [VirtualKeyCode.Add] = Key.Add,
            //[VirtualKeyCode.Apostrophe] = Key.oem,
            [VirtualKeyCode.Apps] = Key.Apps,
            //[VirtualKeyCode.At] ,
            //[VirtualKeyCode.Ax,
            [VirtualKeyCode.Backslash] = Key.OemBackslash,
            //[VirtualKeyCode.Calculator] = Key.,
            [VirtualKeyCode.Capital] = Key.Capital,
            [VirtualKeyCode.Colon] = Key.OemSemicolon,
            [VirtualKeyCode.Comma] = Key.OemComma,
            [VirtualKeyCode.Convert] = Key.ImeConvert,
            [VirtualKeyCode.Decimal] = Key.Decimal,
            [VirtualKeyCode.Divide] = Key.Divide,
            //[VirtualKeyCode.Equals] = Key.,
            //[VirtualKeyCode.Grave] = ,
            [VirtualKeyCode.Kana] = Key.KanaMode,
            [VirtualKeyCode.Kanji] = Key.KanjiMode,
            [VirtualKeyCode.LAlt] = Key.LeftAlt,
            [VirtualKeyCode.LBracket] = Key.OemOpenBrackets,
            [VirtualKeyCode.LControl] = Key.LeftCtrl,
            [VirtualKeyCode.LShift] = Key.LeftShift,
            [VirtualKeyCode.LWin] = Key.LWin,
            [VirtualKeyCode.Mail] = Key.LaunchMail,
            [VirtualKeyCode.MediaSelect] = Key.SelectMedia,
            [VirtualKeyCode.MediaStop] = Key.MediaStop,
            [VirtualKeyCode.Minus] = Key.OemMinus,
            [VirtualKeyCode.Multiply] = Key.Multiply,
            [VirtualKeyCode.Mute] = Key.VolumeMute,
            //[VirtualKeyCode.MyComputer] = Key.,
            [VirtualKeyCode.NavigateForward] = Key.Prior, // also called "Prior"
            [VirtualKeyCode.NavigateBackward] = Key.Next, // also called "Next"
            [VirtualKeyCode.NextTrack] = Key.MediaNextTrack,
            [VirtualKeyCode.NoConvert] = Key.ImeNonConvert,
            [VirtualKeyCode.NumpadComma] = Key.OemComma,
            [VirtualKeyCode.NumpadEnter] = Key.Enter,
            //[VirtualKeyCode.NumpadEquals] = Key.,
            //[VirtualKeyCode.OEM102,
            [VirtualKeyCode.Period] = Key.OemPeriod,
            [VirtualKeyCode.PlayPause] = Key.MediaPlayPause,
            //[VirtualKeyCode.Power] = ,
            [VirtualKeyCode.PrevTrack] = Key.MediaPreviousTrack,
            [VirtualKeyCode.RAlt] = Key.RightAlt,
            [VirtualKeyCode.RBracket] = Key.OemCloseBrackets,
            [VirtualKeyCode.RControl] = Key.RightCtrl,
            [VirtualKeyCode.RShift] = Key.RightShift,
            [VirtualKeyCode.RWin] = Key.RWin,
            [VirtualKeyCode.Semicolon] = Key.OemSemicolon,
            [VirtualKeyCode.Slash] = Key.OemQuestion,
            [VirtualKeyCode.Sleep] = Key.Sleep,
            [VirtualKeyCode.Stop] = Key.MediaStop,
            [VirtualKeyCode.Subtract] = Key.Subtract,
            [VirtualKeyCode.Sysrq] = Key.System,
            [VirtualKeyCode.Tab] = Key.Tab,
            //[VirtualKeyCode.Underline] = Key,
            //[VirtualKeyCode.Unlabeled] = Key.la,
            [VirtualKeyCode.VolumeDown] = Key.VolumeUp,
            [VirtualKeyCode.VolumeUp] = Key.VolumeDown,
            //[VirtualKeyCode.Wake] = Key.,
            [VirtualKeyCode.WebBack] = Key.BrowserBack,
            [VirtualKeyCode.WebFavorites] = Key.BrowserFavorites,
            [VirtualKeyCode.WebForward] = Key.BrowserForward,
            [VirtualKeyCode.WebHome ] = Key.BrowserHome,
            [VirtualKeyCode.WebRefresh] = Key.BrowserRefresh,
            [VirtualKeyCode.WebSearch] = Key.BrowserSearch,
            [VirtualKeyCode.WebStop] = Key.BrowserStop,
            //[VirtualKeyCode.Yen] = Key.,
            [VirtualKeyCode.Copy] = Key.OemCopy,
            //[VirtualKeyCode.Paste] = Key.oem,
            //[VirtualKeyCode.Cut] = Key.cut,
        };

        public static Key? TransformKeyCode(VirtualKeyCode code)
        {
            if (Keys.TryGetValue(code, out var rv))
                return rv;
            return null;
        }
    }
}
