using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.MonoMac
{
    public static class KeyTransform
    {
        // See /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/System/Library/Frameworks/Carbon.framework/Versions/A/Frameworks/HIToolbox.framework/Versions/A/Headers/Events.h
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Local
        private const int kVK_ANSI_A = 0x00;
        private const int kVK_ANSI_S = 0x01;
        private const int kVK_ANSI_D = 0x02;
        private const int kVK_ANSI_F = 0x03;
        private const int kVK_ANSI_H = 0x04;
        private const int kVK_ANSI_G = 0x05;
        private const int kVK_ANSI_Z = 0x06;
        private const int kVK_ANSI_X = 0x07;
        private const int kVK_ANSI_C = 0x08;
        private const int kVK_ANSI_V = 0x09;
        private const int kVK_ANSI_B = 0x0B;
        private const int kVK_ANSI_Q = 0x0C;
        private const int kVK_ANSI_W = 0x0D;
        private const int kVK_ANSI_E = 0x0E;
        private const int kVK_ANSI_R = 0x0F;
        private const int kVK_ANSI_Y = 0x10;
        private const int kVK_ANSI_T = 0x11;
        private const int kVK_ANSI_1 = 0x12;
        private const int kVK_ANSI_2 = 0x13;
        private const int kVK_ANSI_3 = 0x14;
        private const int kVK_ANSI_4 = 0x15;
        private const int kVK_ANSI_6 = 0x16;
        private const int kVK_ANSI_5 = 0x17;
        private const int kVK_ANSI_Equal = 0x18;
        private const int kVK_ANSI_9 = 0x19;
        private const int kVK_ANSI_7 = 0x1A;
        private const int kVK_ANSI_Minus = 0x1B;
        private const int kVK_ANSI_8 = 0x1C;
        private const int kVK_ANSI_0 = 0x1D;
        private const int kVK_ANSI_RightBracket = 0x1E;
        private const int kVK_ANSI_O = 0x1F;
        private const int kVK_ANSI_U = 0x20;
        private const int kVK_ANSI_LeftBracket = 0x21;
        private const int kVK_ANSI_I = 0x22;
        private const int kVK_ANSI_P = 0x23;
        private const int kVK_ANSI_L = 0x25;
        private const int kVK_ANSI_J = 0x26;
        private const int kVK_ANSI_Quote = 0x27;
        private const int kVK_ANSI_K = 0x28;
        private const int kVK_ANSI_Semicolon = 0x29;
        private const int kVK_ANSI_Backslash = 0x2A;
        private const int kVK_ANSI_Comma = 0x2B;
        private const int kVK_ANSI_Slash = 0x2C;
        private const int kVK_ANSI_N = 0x2D;
        private const int kVK_ANSI_M = 0x2E;
        private const int kVK_ANSI_Period = 0x2F;
        private const int kVK_ANSI_Grave = 0x32;
        private const int kVK_ANSI_KeypadDecimal = 0x41;
        private const int kVK_ANSI_KeypadMultiply = 0x43;
        private const int kVK_ANSI_KeypadPlus = 0x45;
        private const int kVK_ANSI_KeypadClear = 0x47;
        private const int kVK_ANSI_KeypadDivide = 0x4B;
        private const int kVK_ANSI_KeypadEnter = 0x4C;
        private const int kVK_ANSI_KeypadMinus = 0x4E;
        private const int kVK_ANSI_KeypadEquals = 0x51;
        private const int kVK_ANSI_Keypad0 = 0x52;
        private const int kVK_ANSI_Keypad1 = 0x53;
        private const int kVK_ANSI_Keypad2 = 0x54;
        private const int kVK_ANSI_Keypad3 = 0x55;
        private const int kVK_ANSI_Keypad4 = 0x56;
        private const int kVK_ANSI_Keypad5 = 0x57;
        private const int kVK_ANSI_Keypad6 = 0x58;
        private const int kVK_ANSI_Keypad7 = 0x59;
        private const int kVK_ANSI_Keypad8 = 0x5B;
        private const int kVK_ANSI_Keypad9 = 0x5C;
        private const int kVK_Return = 0x24;
        private const int kVK_Tab = 0x30;
        private const int kVK_Space = 0x31;
        private const int kVK_Delete = 0x33;
        private const int kVK_Escape = 0x35;
        private const int kVK_Command = 0x37;
        private const int kVK_Shift = 0x38;
        private const int kVK_CapsLock = 0x39;
        private const int kVK_Option = 0x3A;
        private const int kVK_Control = 0x3B;
        private const int kVK_RightCommand = 0x36;
        private const int kVK_RightShift = 0x3C;
        private const int kVK_RightOption = 0x3D;
        private const int kVK_RightControl = 0x3E;
        private const int kVK_Function = 0x3F;
        private const int kVK_F17 = 0x40;
        private const int kVK_VolumeUp = 0x48;
        private const int kVK_VolumeDown = 0x49;
        private const int kVK_Mute = 0x4A;
        private const int kVK_F18 = 0x4F;
        private const int kVK_F19 = 0x50;
        private const int kVK_F20 = 0x5A;
        private const int kVK_F5 = 0x60;
        private const int kVK_F6 = 0x61;
        private const int kVK_F7 = 0x62;
        private const int kVK_F3 = 0x63;
        private const int kVK_F8 = 0x64;
        private const int kVK_F9 = 0x65;
        private const int kVK_F11 = 0x67;
        private const int kVK_F13 = 0x69;
        private const int kVK_F16 = 0x6A;
        private const int kVK_F14 = 0x6B;
        private const int kVK_F10 = 0x6D;
        private const int kVK_F12 = 0x6F;
        private const int kVK_F15 = 0x71;
        private const int kVK_Help = 0x72;
        private const int kVK_Home = 0x73;
        private const int kVK_PageUp = 0x74;
        private const int kVK_ForwardDelete = 0x75;
        private const int kVK_F4 = 0x76;
        private const int kVK_End = 0x77;
        private const int kVK_F2 = 0x78;
        private const int kVK_PageDown = 0x79;
        private const int kVK_F1 = 0x7A;
        private const int kVK_LeftArrow = 0x7B;
        private const int kVK_RightArrow = 0x7C;
        private const int kVK_DownArrow = 0x7D;
        private const int kVK_UpArrow = 0x7E;
        private const int kVK_ISO_Section = 0x0A;
        private const int kVK_JIS_Yen = 0x5D;
        private const int kVK_JIS_Underscore = 0x5E;
        private const int kVK_JIS_KeypadComma = 0x5F;
        private const int kVK_JIS_Eisu = 0x66;
        private const int kVK_JIS_Kana = 0x68;
        // ReSharper restore UnusedMember.Local
        // ReSharper restore InconsistentNaming
        //TODO: Map missing keys
        static readonly Dictionary<int, Key> Keys = new Dictionary<int, Key>
        {
            [kVK_ANSI_A] = Key.A,
            [kVK_ANSI_S] = Key.S,
            [kVK_ANSI_D] = Key.D,
            [kVK_ANSI_F] = Key.F,
            [kVK_ANSI_H] = Key.H,
            [kVK_ANSI_G] = Key.G,
            [kVK_ANSI_Z] = Key.Z,
            [kVK_ANSI_X] = Key.X,
            [kVK_ANSI_C] = Key.C,
            [kVK_ANSI_V] = Key.V,
            [kVK_ANSI_B] = Key.B,
            [kVK_ANSI_Q] = Key.Q,
            [kVK_ANSI_W] = Key.W,
            [kVK_ANSI_E] = Key.E,
            [kVK_ANSI_R] = Key.R,
            [kVK_ANSI_Y] = Key.Y,
            [kVK_ANSI_T] = Key.T,
            [kVK_ANSI_1] = Key.D1,
            [kVK_ANSI_2] = Key.D2,
            [kVK_ANSI_3] = Key.D3,
            [kVK_ANSI_4] = Key.D4,
            [kVK_ANSI_6] = Key.D6,
            [kVK_ANSI_5] = Key.D5,
            //[kVK_ANSI_Equal] = Key.?,
            [kVK_ANSI_9] = Key.D9,
            [kVK_ANSI_7] = Key.D7,
            [kVK_ANSI_Minus] = Key.OemMinus,
            [kVK_ANSI_8] = Key.D8,
            [kVK_ANSI_0] = Key.D0,
            [kVK_ANSI_RightBracket] = Key.OemCloseBrackets,
            [kVK_ANSI_O] = Key.O,
            [kVK_ANSI_U] = Key.U,
            [kVK_ANSI_LeftBracket] = Key.OemOpenBrackets,
            [kVK_ANSI_I] = Key.I,
            [kVK_ANSI_P] = Key.P,
            [kVK_ANSI_L] = Key.L,
            [kVK_ANSI_J] = Key.J,
            [kVK_ANSI_Quote] = Key.OemQuotes,
            [kVK_ANSI_K] = Key.K,
            [kVK_ANSI_Semicolon] = Key.OemSemicolon,
            [kVK_ANSI_Backslash] = Key.OemBackslash,
            [kVK_ANSI_Comma] = Key.OemComma,
            //[kVK_ANSI_Slash] = Key.?,
            [kVK_ANSI_N] = Key.N,
            [kVK_ANSI_M] = Key.M,
            [kVK_ANSI_Period] = Key.OemPeriod,
            //[kVK_ANSI_Grave] = Key.?,
            [kVK_ANSI_KeypadDecimal] =  Key.Decimal,
            [kVK_ANSI_KeypadMultiply] =  Key.Multiply,
            [kVK_ANSI_KeypadPlus] =  Key.OemPlus,
            [kVK_ANSI_KeypadClear] =  Key.Clear,
            [kVK_ANSI_KeypadDivide] =  Key.Divide,
            [kVK_ANSI_KeypadEnter] =  Key.Enter,
            [kVK_ANSI_KeypadMinus] =  Key.OemMinus,
            //[kVK_ANSI_KeypadEquals] =  Key.?,
            [kVK_ANSI_Keypad0] =  Key.NumPad0,
            [kVK_ANSI_Keypad1] =  Key.NumPad1,
            [kVK_ANSI_Keypad2] =  Key.NumPad2,
            [kVK_ANSI_Keypad3] =  Key.NumPad3,
            [kVK_ANSI_Keypad4] =  Key.NumPad4,
            [kVK_ANSI_Keypad5] =  Key.NumPad5,
            [kVK_ANSI_Keypad6] =  Key.NumPad6,
            [kVK_ANSI_Keypad7] =  Key.NumPad7,
            [kVK_ANSI_Keypad8] =  Key.NumPad8,
            [kVK_ANSI_Keypad9] =  Key.NumPad9,
            [kVK_Return] = Key.Return,
            [kVK_Tab] = Key.Tab,
            [kVK_Space] = Key.Space,
            [kVK_Delete] = Key.Delete,
            [kVK_Escape] = Key.Escape,
            [kVK_Command] = Key.LWin,
            [kVK_Shift] = Key.LeftShift,
            [kVK_CapsLock] = Key.CapsLock,
            [kVK_Option] = Key.LeftAlt,
            [kVK_Control] = Key.LeftCtrl,
            [kVK_RightCommand] = Key.RWin,
            [kVK_RightShift] = Key.RightShift,
            [kVK_RightOption] = Key.RightAlt,
            [kVK_RightControl] = Key.RightCtrl,
            //[kVK_Function] = Key.?,
            [kVK_F17] = Key.F17,
            [kVK_VolumeUp] = Key.VolumeUp,
            [kVK_VolumeDown] = Key.VolumeDown,
            [kVK_Mute] = Key.VolumeMute,
            [kVK_F18] = Key.F18,
            [kVK_F19] = Key.F19,
            [kVK_F20] = Key.F20,
            [kVK_F5] = Key.F5,
            [kVK_F6] = Key.F6,
            [kVK_F7] = Key.F7,
            [kVK_F3] = Key.F3,
            [kVK_F8] = Key.F8,
            [kVK_F9] = Key.F9,
            [kVK_F11] = Key.F11,
            [kVK_F13] = Key.F13,
            [kVK_F16] = Key.F16,
            [kVK_F14] = Key.F14,
            [kVK_F10] = Key.F10,
            [kVK_F12] = Key.F12,
            [kVK_F15] = Key.F15,
            [kVK_Help] = Key.Help,
            [kVK_Home] = Key.Home,
            [kVK_PageUp] = Key.PageUp,
            [kVK_ForwardDelete] = Key.Delete,
            [kVK_F4] = Key.F4,
            [kVK_End] = Key.End,
            [kVK_F2] = Key.F2,
            [kVK_PageDown] = Key.PageDown,
            [kVK_F1] = Key.F1,
            [kVK_LeftArrow] = Key.Left,
            [kVK_RightArrow] = Key.Right,
            [kVK_DownArrow] = Key.Down,
            [kVK_UpArrow] = Key.Up,
            /*
            [kVK_ISO_Section] = Key.?,
            [kVK_JIS_Yen] = Key.?,
            [kVK_JIS_Underscore] = Key.?,
            [kVK_JIS_KeypadComma] = Key.?,
            [kVK_JIS_Eisu] = Key.?,
            [kVK_JIS_Kana] = Key.?
            */
        };


        public static Key? TransformKeyCode(ushort code)
        {
            Key rv;
            if (Keys.TryGetValue(code, out rv))
                return rv;
            return null;
        }
    }
}
