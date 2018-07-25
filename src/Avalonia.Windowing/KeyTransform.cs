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

            [VirtualKeyCode.A] = Key.A
        };

        public static Key? TransformKeyCode(VirtualKeyCode code)
        {
            if (Keys.TryGetValue(code, out var rv))
                return rv;
            return null;
        }
    }
}
