// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "KeyTransform.h"

const int kVK_ANSI_A = 0x00;
const int kVK_ANSI_S = 0x01;
const int kVK_ANSI_D = 0x02;
const int kVK_ANSI_F = 0x03;
const int kVK_ANSI_H = 0x04;
const int kVK_ANSI_G = 0x05;
const int kVK_ANSI_Z = 0x06;
const int kVK_ANSI_X = 0x07;
const int kVK_ANSI_C = 0x08;
const int kVK_ANSI_V = 0x09;
const int kVK_ANSI_B = 0x0B;
const int kVK_ANSI_Q = 0x0C;
const int kVK_ANSI_W = 0x0D;
const int kVK_ANSI_E = 0x0E;
const int kVK_ANSI_R = 0x0F;
const int kVK_ANSI_Y = 0x10;
const int kVK_ANSI_T = 0x11;
const int kVK_ANSI_1 = 0x12;
const int kVK_ANSI_2 = 0x13;
const int kVK_ANSI_3 = 0x14;
const int kVK_ANSI_4 = 0x15;
const int kVK_ANSI_6 = 0x16;
const int kVK_ANSI_5 = 0x17;
const int kVK_ANSI_Equal = 0x18;
const int kVK_ANSI_9 = 0x19;
const int kVK_ANSI_7 = 0x1A;
const int kVK_ANSI_Minus = 0x1B;
const int kVK_ANSI_8 = 0x1C;
const int kVK_ANSI_0 = 0x1D;
const int kVK_ANSI_RightBracket = 0x1E;
const int kVK_ANSI_O = 0x1F;
const int kVK_ANSI_U = 0x20;
const int kVK_ANSI_LeftBracket = 0x21;
const int kVK_ANSI_I = 0x22;
const int kVK_ANSI_P = 0x23;
const int kVK_ANSI_L = 0x25;
const int kVK_ANSI_J = 0x26;
const int kVK_ANSI_Quote = 0x27;
const int kVK_ANSI_K = 0x28;
const int kVK_ANSI_Semicolon = 0x29;
const int kVK_ANSI_Backslash = 0x2A;
const int kVK_ANSI_Comma = 0x2B;
const int kVK_ANSI_Slash = 0x2C;
const int kVK_ANSI_N = 0x2D;
const int kVK_ANSI_M = 0x2E;
const int kVK_ANSI_Period = 0x2F;
const int kVK_ANSI_Grave = 0x32;
const int kVK_ANSI_KeypadDecimal = 0x41;
const int kVK_ANSI_KeypadMultiply = 0x43;
const int kVK_ANSI_KeypadPlus = 0x45;
const int kVK_ANSI_KeypadClear = 0x47;
const int kVK_ANSI_KeypadDivide = 0x4B;
const int kVK_ANSI_KeypadEnter = 0x4C;
const int kVK_ANSI_KeypadMinus = 0x4E;
const int kVK_ANSI_KeypadEquals = 0x51;
const int kVK_ANSI_Keypad0 = 0x52;
const int kVK_ANSI_Keypad1 = 0x53;
const int kVK_ANSI_Keypad2 = 0x54;
const int kVK_ANSI_Keypad3 = 0x55;
const int kVK_ANSI_Keypad4 = 0x56;
const int kVK_ANSI_Keypad5 = 0x57;
const int kVK_ANSI_Keypad6 = 0x58;
const int kVK_ANSI_Keypad7 = 0x59;
const int kVK_ANSI_Keypad8 = 0x5B;
const int kVK_ANSI_Keypad9 = 0x5C;
const int kVK_Return = 0x24;
const int kVK_Tab = 0x30;
const int kVK_Space = 0x31;
const int kVK_Delete = 0x33;
const int kVK_Escape = 0x35;
const int kVK_Command = 0x37;
const int kVK_Shift = 0x38;
const int kVK_CapsLock = 0x39;
const int kVK_Option = 0x3A;
const int kVK_Control = 0x3B;
const int kVK_RightCommand = 0x36;
const int kVK_RightShift = 0x3C;
const int kVK_RightOption = 0x3D;
const int kVK_RightControl = 0x3E;
//const int kVK_Function = 0x3F;
const int kVK_F17 = 0x40;
const int kVK_VolumeUp = 0x48;
const int kVK_VolumeDown = 0x49;
const int kVK_Mute = 0x4A;
const int kVK_F18 = 0x4F;
const int kVK_F19 = 0x50;
const int kVK_F20 = 0x5A;
const int kVK_F5 = 0x60;
const int kVK_F6 = 0x61;
const int kVK_F7 = 0x62;
const int kVK_F3 = 0x63;
const int kVK_F8 = 0x64;
const int kVK_F9 = 0x65;
const int kVK_F11 = 0x67;
const int kVK_F13 = 0x69;
const int kVK_F16 = 0x6A;
const int kVK_F14 = 0x6B;
const int kVK_F10 = 0x6D;
const int kVK_F12 = 0x6F;
const int kVK_F15 = 0x71;
const int kVK_Help = 0x72;
const int kVK_Home = 0x73;
const int kVK_PageUp = 0x74;
const int kVK_ForwardDelete = 0x75;
const int kVK_F4 = 0x76;
const int kVK_End = 0x77;
const int kVK_F2 = 0x78;
const int kVK_PageDown = 0x79;
const int kVK_F1 = 0x7A;
const int kVK_LeftArrow = 0x7B;
const int kVK_RightArrow = 0x7C;
const int kVK_DownArrow = 0x7D;
const int kVK_UpArrow = 0x7E;
//const int kVK_ISO_Section = 0x0A;
//const int kVK_JIS_Yen = 0x5D;
//const int kVK_JIS_Underscore = 0x5E;
//const int kVK_JIS_KeypadComma = 0x5F;
//const int kVK_JIS_Eisu = 0x66;
const int kVK_JIS_Kana = 0x68;

 std::map<int, AvnKey> s_KeyMap =
 {
    {kVK_ANSI_A, A},
    {kVK_ANSI_S, S},
    {kVK_ANSI_D, D},
    {kVK_ANSI_F, F},
    {kVK_ANSI_H, H},
    {kVK_ANSI_G, G},
    {kVK_ANSI_Z, Z},
    {kVK_ANSI_X, X},
    {kVK_ANSI_C, C},
    {kVK_ANSI_V, V},
    {kVK_ANSI_B, B},
    {kVK_ANSI_Q, Q},
    {kVK_ANSI_W, W},
    {kVK_ANSI_E, E},
    {kVK_ANSI_R, R},
    {kVK_ANSI_Y, Y},
    {kVK_ANSI_T, T},
    {kVK_ANSI_1, D1},
    {kVK_ANSI_2, D2},
    {kVK_ANSI_3, D3},
    {kVK_ANSI_4, D4},
    {kVK_ANSI_6, D6},
    {kVK_ANSI_5, D5},
    {kVK_ANSI_Equal, OemPlus},
    {kVK_ANSI_9, D9},
    {kVK_ANSI_7, D7},
    {kVK_ANSI_Minus, OemMinus},
    {kVK_ANSI_8, D8},
    {kVK_ANSI_0, D0},
    {kVK_ANSI_RightBracket, OemCloseBrackets},
    {kVK_ANSI_O, O},
    {kVK_ANSI_U, U},
    {kVK_ANSI_LeftBracket, OemOpenBrackets},
    {kVK_ANSI_I, I},
    {kVK_ANSI_P, P},
    {kVK_ANSI_L, L},
    {kVK_ANSI_J, J},
    {kVK_ANSI_Quote, OemQuotes},
    {kVK_ANSI_K, AvnKeyK},
    {kVK_ANSI_Semicolon, OemSemicolon},
    {kVK_ANSI_Backslash, OemBackslash},
    {kVK_ANSI_Comma, OemComma},
    {kVK_ANSI_Slash, Oem2},
    {kVK_ANSI_N, N},
    {kVK_ANSI_M, M},
    {kVK_ANSI_Period, OemPeriod},
    {kVK_ANSI_Grave, OemTilde},
    {kVK_ANSI_KeypadDecimal, Decimal},
    {kVK_ANSI_KeypadMultiply, Multiply},
    {kVK_ANSI_KeypadPlus, OemPlus},
    {kVK_ANSI_KeypadClear, AvnKeyClear},
    {kVK_ANSI_KeypadDivide, Divide},
    {kVK_ANSI_KeypadEnter, AvnKeyEnter},
    {kVK_ANSI_KeypadMinus, OemMinus},
    {kVK_ANSI_KeypadEquals, OemPlus},
    {kVK_ANSI_Keypad0, NumPad0},
    {kVK_ANSI_Keypad1, NumPad1},
    {kVK_ANSI_Keypad2, NumPad2},
    {kVK_ANSI_Keypad3, NumPad3},
    {kVK_ANSI_Keypad4, NumPad4},
    {kVK_ANSI_Keypad5, NumPad5},
    {kVK_ANSI_Keypad6, NumPad6},
    {kVK_ANSI_Keypad7, NumPad7},
    {kVK_ANSI_Keypad8, NumPad8},
    {kVK_ANSI_Keypad9, NumPad9},
    {kVK_Return, AvnKeyReturn},
    {kVK_Tab, AvnKeyTab},
    {kVK_Space, Space},
    {kVK_Delete, AvnKeyBack},
    {kVK_Escape, Escape},
    {kVK_Command, LWin},
    {kVK_Shift, LeftShift},
    {kVK_CapsLock, AvnKeyCapsLock},
    {kVK_Option, LeftAlt},
    {kVK_Control, LeftCtrl},
    {kVK_RightCommand, RWin},
    {kVK_RightShift, RightShift},
    {kVK_RightOption, RightAlt},
    {kVK_RightControl, RightCtrl},
    //{kVK_Function, ?},
    {kVK_F17, F17},
    {kVK_VolumeUp, VolumeUp},
    {kVK_VolumeDown, VolumeDown},
    {kVK_Mute, VolumeMute},
    {kVK_F18, F18},
    {kVK_F19, F19},
    {kVK_F20, F20},
    {kVK_F5, F5},
    {kVK_F6, F6},
    {kVK_F7, F7},
    {kVK_F3, F3},
    {kVK_F8, F8},
    {kVK_F9, F9},
    {kVK_F11, F11},
    {kVK_F13, F13},
    {kVK_F16, F16},
    {kVK_F14, F14},
    {kVK_F10, F10},
    {kVK_F12, F12},
    {kVK_F15, F15},
    {kVK_Help, Help},
    {kVK_Home, Home},
    {kVK_PageUp, PageUp},
    {kVK_ForwardDelete, Delete},
    {kVK_F4, F4},
    {kVK_End, End},
    {kVK_F2, F2},
    {kVK_PageDown, PageDown},
    {kVK_F1, F1},
    {kVK_LeftArrow, Left},
    {kVK_RightArrow, Right},
    {kVK_DownArrow, Down},
    {kVK_UpArrow, Up},
    {kVK_JIS_Kana, AvnKeyKanaMode},
};
