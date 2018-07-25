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

            [VirtualKeyCode.Back] = Key.Back,
            [VirtualKeyCode.Escape] = Key.Escape,
            [VirtualKeyCode.Return] = Key.Return,

            [VirtualKeyCode.Down] = Key.Down,
            [VirtualKeyCode.Up] = Key.Up,
            [VirtualKeyCode.Right] = Key.Right,
            [VirtualKeyCode.Left] = Key.Left,

            [VirtualKeyCode.Capital] = Key.CapsLock,
            [VirtualKeyCode.LAlt]= Key.LeftAlt,
            [VirtualKeyCode.RAlt] = Key.RightAlt,
            [VirtualKeyCode.LControl] = Key.LeftCtrl,
            [VirtualKeyCode.RControl] = Key.RightCtrl,
            [VirtualKeyCode.LShift] = Key.LeftShift,
            [VirtualKeyCode.RShift] = Key.RightShift,
            [VirtualKeyCode.LWin] = Key.LWin,
            [VirtualKeyCode.RWin] = Key.RWin,
            [VirtualKeyCode.Numlock] = Key.NumLock,

            [VirtualKeyCode.Add] = Key.OemPlus,
            [VirtualKeyCode.Subtract] = Key.Subtract,
            [VirtualKeyCode.Backslash] = Key.OemBackslash,
            [VirtualKeyCode.Tab] = Key.Tab,

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
            [VirtualKeyCode.X] = Key.Y,
            [VirtualKeyCode.X] = Key.Z,
        };

        public static Key? TransformKeyCode(VirtualKeyCode code)
        {
            if (Keys.TryGetValue(code, out var rv))
                return rv;
            return null;
        }
    }
}
