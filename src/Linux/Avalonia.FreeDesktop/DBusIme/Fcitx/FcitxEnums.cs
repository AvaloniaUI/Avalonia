using System;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal enum FcitxKeyEventType
    {
        FCITX_PRESS_KEY,
        FCITX_RELEASE_KEY
    }

    [Flags]
    internal enum FcitxCapabilityFlags
    {
        CAPACITY_NONE = 0,
        CAPACITY_CLIENT_SIDE_UI = 1 << 0,
        CAPACITY_PREEDIT = 1 << 1,
        CAPACITY_CLIENT_SIDE_CONTROL_STATE = 1 << 2,
        CAPACITY_PASSWORD = 1 << 3,
        CAPACITY_FORMATTED_PREEDIT = 1 << 4,
        CAPACITY_CLIENT_UNFOCUS_COMMIT = 1 << 5,
        CAPACITY_SURROUNDING_TEXT = 1 << 6,
        CAPACITY_EMAIL = 1 << 7,
        CAPACITY_DIGIT = 1 << 8,
        CAPACITY_UPPERCASE = 1 << 9,
        CAPACITY_LOWERCASE = 1 << 10,
        CAPACITY_NOAUTOUPPERCASE = 1 << 11,
        CAPACITY_URL = 1 << 12,
        CAPACITY_DIALABLE = 1 << 13,
        CAPACITY_NUMBER = 1 << 14,
        CAPACITY_NO_ON_SCREEN_KEYBOARD = 1 << 15,
        CAPACITY_SPELLCHECK = 1 << 16,
        CAPACITY_NO_SPELLCHECK = 1 << 17,
        CAPACITY_WORD_COMPLETION = 1 << 18,
        CAPACITY_UPPERCASE_WORDS = 1 << 19,
        CAPACITY_UPPERCASE_SENTENCES = 1 << 20,
        CAPACITY_ALPHA = 1 << 21,
        CAPACITY_NAME = 1 << 22,
        CAPACITY_GET_IM_INFO_ON_FOCUS = 1 << 23,
        CAPACITY_RELATIVE_CURSOR_RECT = 1 << 24
    }

    [Flags]
    internal enum FcitxKeyState
    {
        FcitxKeyState_None = 0,
        FcitxKeyState_Shift = 1 << 0,
        FcitxKeyState_CapsLock = 1 << 1,
        FcitxKeyState_Ctrl = 1 << 2,
        FcitxKeyState_Alt = 1 << 3,
        FcitxKeyState_Alt_Shift = FcitxKeyState_Alt | FcitxKeyState_Shift,
        FcitxKeyState_Ctrl_Shift = FcitxKeyState_Ctrl | FcitxKeyState_Shift,
        FcitxKeyState_Ctrl_Alt = FcitxKeyState_Ctrl | FcitxKeyState_Alt,

        FcitxKeyState_Ctrl_Alt_Shift =
            FcitxKeyState_Ctrl | FcitxKeyState_Alt | FcitxKeyState_Shift,
        FcitxKeyState_NumLock = 1 << 4,
        FcitxKeyState_Super = 1 << 6,
        FcitxKeyState_ScrollLock = 1 << 7,
        FcitxKeyState_MousePressed = 1 << 8,
        FcitxKeyState_HandledMask = 1 << 24,
        FcitxKeyState_IgnoredMask = 1 << 25,
        FcitxKeyState_Super2 = 1 << 26,
        FcitxKeyState_Hyper = 1 << 27,
        FcitxKeyState_Meta = 1 << 28,
        FcitxKeyState_UsedMask = 0x5c001fff
    }
}
