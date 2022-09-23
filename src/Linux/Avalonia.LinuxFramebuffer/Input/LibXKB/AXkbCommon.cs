using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using static Avalonia.LinuxFramebuffer.Input.LibXKB.LibXKBNative;
using static Avalonia.LinuxFramebuffer.Input.LibXKB.XKBKeys;

namespace Avalonia.LinuxFramebuffer.Input.LibXKB;

internal static class AXkbCommon
{
    private const uint F1 = (uint)Key.F1;

    private static readonly Dictionary<uint, Key> s_SysKeyToAvKeyMap = new()
    {
        {XKB_KEY_Escape,                  Key.Escape},
        {XKB_KEY_Tab,                     Key.Tab},
        {XKB_KEY_ISO_Left_Tab,            Key.OemBackTab},
        {XKB_KEY_BackSpace,               Key.Delete},         //TODO: no found avalonia
        {XKB_KEY_Return,                  Key.Return},
        {XKB_KEY_Insert,                  Key.Insert},
        {XKB_KEY_Delete,                  Key.Delete},
        {XKB_KEY_Clear,                   Key.Delete},
        {XKB_KEY_Pause,                   Key.Pause},
        {XKB_KEY_Print,                   Key.Print},
        /* TODO: no found avalonia 
        {XKB_KEY_Sys_Req,                 Key.SysReq},
        {0x1005FF60,                      Key.SysReq},         // hardcoded Sun SysReq
        {0x1007ff00,                      Key.SysReq},         // hardcoded X386 SysReq
        */
        // cursor movement

        {XKB_KEY_Home,                    Key.Home},
        {XKB_KEY_End,                     Key.End},
        {XKB_KEY_Left,                    Key.Left},
        {XKB_KEY_Up,                      Key.Up},
        {XKB_KEY_Right,                   Key.Right},
        {XKB_KEY_Down,                    Key.Down},
        {XKB_KEY_Prior,                   Key.PageUp},
        {XKB_KEY_Next,                    Key.PageDown},

        // modifiers

        {XKB_KEY_Shift_L,                 Key.LeftShift},
        {XKB_KEY_Shift_R,                 Key.RightShift},
        {XKB_KEY_Shift_Lock,              Key.CapsLock},
        {XKB_KEY_Control_L,               Key.LeftCtrl},
        {XKB_KEY_Control_R,               Key.RightCtrl},
        {XKB_KEY_Meta_L,                  Key.LWin},
        {XKB_KEY_Meta_R,                  Key.RWin},
        {XKB_KEY_Alt_L,                   Key.LeftAlt},
        {XKB_KEY_Alt_R,                   Key.RightAlt},
        {XKB_KEY_Caps_Lock,               Key.CapsLock},
        {XKB_KEY_Num_Lock,                Key.NumLock},
        {XKB_KEY_Scroll_Lock,             Key.Scroll},
        {XKB_KEY_Super_L,                 Key.LWin},
        {XKB_KEY_Super_R,                 Key.RWin},
        {XKB_KEY_Menu,                    Key.Apps},
        //{XKB_KEY_Hyper_L,                 Key.Hyper_L},       // TODO: no found avalonia
        //{XKB_KEY_Hyper_R,                 Key.Hyper_R},       // TODO: no found avalonia
        {XKB_KEY_Help,                    Key.Help},
        {0x1000FF74,                      Key.OemBackTab},     // hardcoded HP backtab
        {0x1005FF10,                      Key.F11},            // hardcoded Sun F36 {labeled F11}
        {0x1005FF11,                      Key.F12},            // hardcoded Sun F37 {labeled F12}

        // numeric and function keypad keys

        {XKB_KEY_KP_Space,                Key.Space},
        {XKB_KEY_KP_Tab,                  Key.Tab},
        {XKB_KEY_KP_Enter,                Key.Enter},
        {XKB_KEY_KP_Home,                 Key.Home},
        {XKB_KEY_KP_Left,                 Key.Left},
        {XKB_KEY_KP_Up,                   Key.Up},
        {XKB_KEY_KP_Right,                Key.Right},
        {XKB_KEY_KP_Down,                 Key.Down},
        {XKB_KEY_KP_Prior,                Key.PageUp},
        {XKB_KEY_KP_Next,                 Key.PageDown},
        {XKB_KEY_KP_End,                  Key.End},
        {XKB_KEY_KP_Begin,                Key.Clear},
        {XKB_KEY_KP_Insert,               Key.Insert},
        {XKB_KEY_KP_Delete,               Key.Delete},
    //    {XKB_KEY_KP_Equal,                Key.Equal}, // TODO: no found avalonia
    //    {XKB_KEY_KP_Multiply,             Key.Asterisk},// TODO: no found avalonia
        {XKB_KEY_KP_Add,                  Key.OemPlus},
        {XKB_KEY_KP_Separator,            Key.OemComma},
        {XKB_KEY_KP_Subtract,             Key.OemMinus},
        {XKB_KEY_KP_Decimal,              Key.Decimal},
        {XKB_KEY_KP_Divide,               Key.Divide},

        // special non-XF86 function keys
        /* TODO: no found avalonia
        {XKB_KEY_Undo,                    Key.Undo},
        {XKB_KEY_Redo,                    Key.Redo},
        {XKB_KEY_Find,                    Key.Find},
        {XKB_KEY_Cancel,                  Key.Cancel},

        // International input method support keys

        // International & multi-key character composition
        {XKB_KEY_ISO_Level3_Shift,        Key.AltGr},
        {XKB_KEY_Multi_key,               Key.Multi_key},
        {XKB_KEY_Codeinput,               Key.Codeinput},
        {XKB_KEY_SingleCandidate,         Key.SingleCandidate},
        {XKB_KEY_MultipleCandidate,       Key.MultipleCandidate},
        {XKB_KEY_PreviousCandidate,       Key.PreviousCandidate},
        
        // Misc Functions
        {XKB_KEY_Mode_switch,             Key.Mode_switch},
        {XKB_KEY_script_switch,           Key.Mode_switch},
        */
        // Japanese keyboard support
        {XKB_KEY_Kanji,                   Key.KanjiMode},
        //{XKB_KEY_Muhenkan,                Key.Muhenkan}, TODO: no found avalonia
        //{XKB_KEY_Henkan_Mode,           Key.Henkan_Mode},
        /* TODO: no found avalonia
        {XKB_KEY_Henkan_Mode,             Key.Henkan},
        {XKB_KEY_Henkan,                  Key.Henkan},
        {XKB_KEY_Romaji,                  Key.Romaji},
        */
        {XKB_KEY_Hiragana,                Key.DbeHiragana},
        {XKB_KEY_Katakana,                Key.DbeKatakana},
        {XKB_KEY_Hiragana_Katakana,       Key.DbeHiragana | Key.DbeKatakana},
        // {XKB_KEY_Zenkaku,                 Key.Zenkaku}, TODO: no found avalonia
        {XKB_KEY_Hankaku,                 Key.HangulMode},
        /* TODO: no found avalonia
        {XKB_KEY_Zenkaku_Hankaku,         Key.Zenkaku_Hankaku},
        {XKB_KEY_Touroku,                 Key.Touroku},
        {XKB_KEY_Massyo,                  Key.Massyo},

        {XKB_KEY_Kana_Lock,               Key.Kana_Lock},
        {XKB_KEY_Kana_Shift,              Key.Kana_Shift},
        {XKB_KEY_Eisu_Shift,              Key.Eisu_Shift},
        {XKB_KEY_Eisu_toggle,             Key.Eisu_toggle},
        //{XKB_KEY_Kanji_Bangou,          Key.Kanji_Bangou},
        //{XKB_KEY_Zen_Koho,              Key.Zen_Koho},
        //{XKB_KEY_Mae_Koho,              Key.Mae_Koho},
        {XKB_KEY_Kanji_Bangou,            Key.Codeinput},
        {XKB_KEY_Zen_Koho,                Key.MultipleCandidate},
        {XKB_KEY_Mae_Koho,                Key.PreviousCandidate},
       
        // Korean keyboard support
        {XKB_KEY_Hangul,                  Key.Hangul},
        {XKB_KEY_Hangul_Start,            Key.Hangul_Start},
        {XKB_KEY_Hangul_End,              Key.Hangul_End},
        {XKB_KEY_Hangul_Hanja,            Key.Hangul_Hanja},
        {XKB_KEY_Hangul_Jamo,             Key.Hangul_Jamo},
        {XKB_KEY_Hangul_Romaja,           Key.Hangul_Romaja},
        //{XKB_KEY_Hangul_Codeinput,      Key.Hangul_Codeinput},
        {XKB_KEY_Hangul_Codeinput,        Key.Codeinput},
        {XKB_KEY_Hangul_Jeonja,           Key.Hangul_Jeonja},
        {XKB_KEY_Hangul_Banja,            Key.Hangul_Banja},
        {XKB_KEY_Hangul_PreHanja,         Key.Hangul_PreHanja},
        {XKB_KEY_Hangul_PostHanja,        Key.Hangul_PostHanja},
        //{XKB_KEY_Hangul_SingleCandidate,Key.Hangul_SingleCandidate},
        //{XKB_KEY_Hangul_MultipleCandidate,Key.Hangul_MultipleCandidate},
        //{XKB_KEY_Hangul_PreviousCandidate,Key.Hangul_PreviousCandidate},
        {XKB_KEY_Hangul_SingleCandidate,  Key.SingleCandidate},
        {XKB_KEY_Hangul_MultipleCandidate,Key.MultipleCandidate},
        {XKB_KEY_Hangul_PreviousCandidate,Key.PreviousCandidate},
        {XKB_KEY_Hangul_Special,          Key.Hangul_Special},
        //{XKB_KEY_Hangul_switch,         Key.Hangul_switch},
        {XKB_KEY_Hangul_switch,           Key.Mode_switch},

        // dead keys
        {XKB_KEY_dead_grave,              Key.Dead_Grave},
        {XKB_KEY_dead_acute,              Key.Dead_Acute},
        {XKB_KEY_dead_circumflex,         Key.Dead_Circumflex},
        {XKB_KEY_dead_tilde,              Key.Dead_Tilde},
        {XKB_KEY_dead_macron,             Key.Dead_Macron},
        {XKB_KEY_dead_breve,              Key.Dead_Breve},
        {XKB_KEY_dead_abovedot,           Key.Dead_Abovedot},
        {XKB_KEY_dead_diaeresis,          Key.Dead_Diaeresis},
        {XKB_KEY_dead_abovering,          Key.Dead_Abovering},
        {XKB_KEY_dead_doubleacute,        Key.Dead_Doubleacute},
        {XKB_KEY_dead_caron,              Key.Dead_Caron},
        {XKB_KEY_dead_cedilla,            Key.Dead_Cedilla},
        {XKB_KEY_dead_ogonek,             Key.Dead_Ogonek},
        {XKB_KEY_dead_iota,               Key.Dead_Iota},
        {XKB_KEY_dead_voiced_sound,       Key.Dead_Voiced_Sound},
        {XKB_KEY_dead_semivoiced_sound,   Key.Dead_Semivoiced_Sound},
        {XKB_KEY_dead_belowdot,           Key.Dead_Belowdot},
        {XKB_KEY_dead_hook,               Key.Dead_Hook},
        {XKB_KEY_dead_horn,               Key.Dead_Horn},
        {XKB_KEY_dead_stroke,             Key.Dead_Stroke},
        {XKB_KEY_dead_abovecomma,         Key.Dead_Abovecomma},
        {XKB_KEY_dead_abovereversedcomma, Key.Dead_Abovereversedcomma},
        {XKB_KEY_dead_doublegrave,        Key.Dead_Doublegrave},
        {XKB_KEY_dead_belowring,          Key.Dead_Belowring},
        {XKB_KEY_dead_belowmacron,        Key.Dead_Belowmacron},
        {XKB_KEY_dead_belowcircumflex,    Key.Dead_Belowcircumflex},
        {XKB_KEY_dead_belowtilde,         Key.Dead_Belowtilde},
        {XKB_KEY_dead_belowbreve,         Key.Dead_Belowbreve},
        {XKB_KEY_dead_belowdiaeresis,     Key.Dead_Belowdiaeresis},
        {XKB_KEY_dead_invertedbreve,      Key.Dead_Invertedbreve},
        {XKB_KEY_dead_belowcomma,         Key.Dead_Belowcomma},
        {XKB_KEY_dead_currency,           Key.Dead_Currency},
        {XKB_KEY_dead_a,                  Key.Dead_a},
        {XKB_KEY_dead_A,                  Key.Dead_A},
        {XKB_KEY_dead_e,                  Key.Dead_e},
        {XKB_KEY_dead_E,                  Key.Dead_E},
        {XKB_KEY_dead_i,                  Key.Dead_i},
        {XKB_KEY_dead_I,                  Key.Dead_I},
        {XKB_KEY_dead_o,                  Key.Dead_o},
        {XKB_KEY_dead_O,                  Key.Dead_O},
        {XKB_KEY_dead_u,                  Key.Dead_u},
        {XKB_KEY_dead_U,                  Key.Dead_U},
        {XKB_KEY_dead_small_schwa,        Key.Dead_Small_Schwa},
        {XKB_KEY_dead_capital_schwa,      Key.Dead_Capital_Schwa},
        {XKB_KEY_dead_greek,              Key.Dead_Greek},
        {XKB_KEY_dead_lowline,            Key.Dead_Lowline},
        {XKB_KEY_dead_aboveverticalline,  Key.Dead_Aboveverticalline},
        {XKB_KEY_dead_belowverticalline,  Key.Dead_Belowverticalline},
        {XKB_KEY_dead_longsolidusoverlay, Key.Dead_Longsolidusoverlay},
        */

        // Special keys from X.org - This include multimedia keys,
        // wireless/bluetooth/uwb keys, special launcher keys, etc.
        {XKB_KEY_XF86Back,                Key.BrowserBack},
        {XKB_KEY_XF86Forward,             Key.BrowserForward},
        {XKB_KEY_XF86Stop,                Key.BrowserStop},
        {XKB_KEY_XF86Refresh,             Key.BrowserRefresh},
        {XKB_KEY_XF86Favorites,           Key.BrowserFavorites},
        {XKB_KEY_XF86AudioMedia,          Key.BrowserHome},
        // {XKB_KEY_XF86OpenURL,             Key.OpenUrl}, // TODO: no found avalonia
        {XKB_KEY_XF86HomePage,            Key.BrowserHome},
        {XKB_KEY_XF86Search,              Key.BrowserSearch},
        {XKB_KEY_XF86AudioLowerVolume,    Key.VolumeDown},
        {XKB_KEY_XF86AudioMute,           Key.VolumeMute},
        {XKB_KEY_XF86AudioRaiseVolume,    Key.VolumeUp},
        {XKB_KEY_XF86AudioPlay,           Key.MediaPlayPause},
        {XKB_KEY_XF86AudioStop,           Key.MediaStop},
        {XKB_KEY_XF86AudioPrev,           Key.MediaPreviousTrack},
        {XKB_KEY_XF86AudioNext,           Key.MediaNextTrack},
        // {XKB_KEY_XF86AudioRecord,         Key.MediaRecord}, // TODO: no found avalonia
        {XKB_KEY_XF86AudioPause,          Key.MediaPlayPause},
        {XKB_KEY_XF86Mail,                Key.LaunchMail},
        /* TODO: no found avalonia
        {XKB_KEY_XF86MyComputer,          Key.LaunchMedia},
        {XKB_KEY_XF86Memo,                Key.Memo},
        {XKB_KEY_XF86ToDoList,            Key.ToDoList},
        {XKB_KEY_XF86Calendar,            Key.Calendar},
        {XKB_KEY_XF86PowerDown,           Key.PowerDown},
        {XKB_KEY_XF86ContrastAdjust,      Key.ContrastAdjust},
        {XKB_KEY_XF86Standby,             Key.Standby},
        {XKB_KEY_XF86MonBrightnessUp,     Key.MonBrightnessUp},
        {XKB_KEY_XF86MonBrightnessDown,   Key.MonBrightnessDown},
        {XKB_KEY_XF86KbdLightOnOff,       Key.KeyboardLightOnOff},
        {XKB_KEY_XF86KbdBrightnessUp,     Key.KeyboardBrightnessUp},
        {XKB_KEY_XF86KbdBrightnessDown,   Key.KeyboardBrightnessDown},
        {XKB_KEY_XF86PowerOff,            Key.PowerOff},
        {XKB_KEY_XF86WakeUp,              Key.WakeUp},
        {XKB_KEY_XF86Eject,               Key.Eject},
        {XKB_KEY_XF86ScreenSaver,         Key.ScreenSaver},
        {XKB_KEY_XF86WWW,                 Key.WWW},
        */
        {XKB_KEY_XF86Sleep,               Key.Sleep},
        /* TODO: no found avalonia
        {XKB_KEY_XF86LightBulb,           Key.LightBulb},
        {XKB_KEY_XF86Shop,                Key.Shop},
        {XKB_KEY_XF86History,             Key.History},
        {XKB_KEY_XF86AddFavorite,         Key.AddFavorite},
        {XKB_KEY_XF86HotLinks,            Key.HotLinks},
        {XKB_KEY_XF86BrightnessAdjust,    Key.BrightnessAdjust},
        {XKB_KEY_XF86Finance,             Key.Finance},
        {XKB_KEY_XF86Community,           Key.Community},
        {XKB_KEY_XF86AudioRewind,         Key.AudioRewind},
        {XKB_KEY_XF86BackForward,         Key.BackForward},
        {XKB_KEY_XF86ApplicationLeft,     Key.ApplicationLeft},
        {XKB_KEY_XF86ApplicationRight,    Key.ApplicationRight},
        {XKB_KEY_XF86Book,                Key.Book},
        {XKB_KEY_XF86CD,                  Key.CD},
        
        {XKB_KEY_XF86Calculater,          Key.Calculator},
        */
        {XKB_KEY_XF86Clear,               Key.Clear},
        
        /* TODO: no found avalonia
        {XKB_KEY_XF86ClearGrab,           Key.ClearGrab},
        {XKB_KEY_XF86Close,               Key.Close},
        */
        {XKB_KEY_XF86Copy,                Key.OemCopy},
        /* TODO: no found avalonia
        {XKB_KEY_XF86Cut,                 Key.Cut},
        {XKB_KEY_XF86Display,             Key.Display},
        {XKB_KEY_XF86DOS,                 Key.DOS},
        {XKB_KEY_XF86Documents,           Key.Documents},
       
        {XKB_KEY_XF86Excel,               Key.Excel},
        {XKB_KEY_XF86Explorer,            Key.Explorer},
        {XKB_KEY_XF86Game,                Key.Game},
        {XKB_KEY_XF86Go,                  Key.Go},
        {XKB_KEY_XF86iTouch,              Key.iTouch},
        {XKB_KEY_XF86LogOff,              Key.LogOff},
        {XKB_KEY_XF86Market,              Key.Market},
        {XKB_KEY_XF86Meeting,             Key.Meeting},
        {XKB_KEY_XF86MenuKB,              Key.MenuKB},
        {XKB_KEY_XF86MenuPB,              Key.MenuPB},
        {XKB_KEY_XF86MySites,             Key.MySites},
        {XKB_KEY_XF86New,                 Key.New},
        {XKB_KEY_XF86News,                Key.News},
        {XKB_KEY_XF86OfficeHome,          Key.OfficeHome},
        {XKB_KEY_XF86Open,                Key.Open},
        {XKB_KEY_XF86Option,              Key.Option},
        {XKB_KEY_XF86Paste,               Key.Paste},
        {XKB_KEY_XF86Phone,               Key.Phone},
        {XKB_KEY_XF86Reply,               Key.Reply},
        {XKB_KEY_XF86Reload,              Key.Reload},
        {XKB_KEY_XF86RotateWindows,       Key.RotateWindows},
        {XKB_KEY_XF86RotationPB,          Key.RotationPB},
        {XKB_KEY_XF86RotationKB,          Key.RotationKB},
        {XKB_KEY_XF86Save,                Key.Save},
        {XKB_KEY_XF86Send,                Key.Send},
        {XKB_KEY_XF86Spell,               Key.Spell},
        {XKB_KEY_XF86SplitScreen,         Key.SplitScreen},
        {XKB_KEY_XF86Support,             Key.Support},
        {XKB_KEY_XF86TaskPane,            Key.TaskPane},
        {XKB_KEY_XF86Terminal,            Key.Terminal},
        {XKB_KEY_XF86Tools,               Key.Tools},
        {XKB_KEY_XF86Travel,              Key.Travel},
        {XKB_KEY_XF86Video,               Key.Video},
        {XKB_KEY_XF86Word,                Key.Word},
        {XKB_KEY_XF86Xfer,                Key.Xfer},
        {XKB_KEY_XF86ZoomIn,              Key.ZoomIn},
        {XKB_KEY_XF86ZoomOut,             Key.ZoomOut},
        {XKB_KEY_XF86Away,                Key.Away},
        {XKB_KEY_XF86Messenger,           Key.Messenger},
        {XKB_KEY_XF86WebCam,              Key.WebCam},
        {XKB_KEY_XF86MailForward,         Key.MailForward},
        {XKB_KEY_XF86Pictures,            Key.Pictures},
        {XKB_KEY_XF86Music,               Key.Music},
        {XKB_KEY_XF86Battery,             Key.Battery},
        {XKB_KEY_XF86Bluetooth,           Key.Bluetooth},
        {XKB_KEY_XF86WLAN,                Key.WLAN},
        {XKB_KEY_XF86UWB,                 Key.UWB},
        {XKB_KEY_XF86AudioForward,        Key.AudioForward},
        {XKB_KEY_XF86AudioRepeat,         Key.AudioRepeat},
        {XKB_KEY_XF86AudioRandomPlay,     Key.AudioRandomPlay},
        {XKB_KEY_XF86Subtitle,            Key.Subtitle},
        {XKB_KEY_XF86AudioCycleTrack,     Key.AudioCycleTrack},
        {XKB_KEY_XF86Time,                Key.Time},
        */
        {XKB_KEY_XF86Select,              Key.Select},

        /* TODO: no found avalonia
        {XKB_KEY_XF86View,                Key.View},
        {XKB_KEY_XF86TopMenu,             Key.TopMenu},
        {XKB_KEY_XF86Red,                 Key.Red},
        {XKB_KEY_XF86Green,               Key.Green},
        {XKB_KEY_XF86Yellow,              Key.Yellow},
        {XKB_KEY_XF86Blue,                Key.Blue},
        {XKB_KEY_XF86Bluetooth,           Key.Bluetooth},
        {XKB_KEY_XF86Suspend,             Key.Suspend},
        {XKB_KEY_XF86Hibernate,           Key.Hibernate},
        {XKB_KEY_XF86TouchpadToggle,      Key.TouchpadToggle},
        {XKB_KEY_XF86TouchpadOn,          Key.TouchpadOn},
        {XKB_KEY_XF86TouchpadOff,         Key.TouchpadOff},
        {XKB_KEY_XF86AudioMicMute,        Key.MicMute},
        {XKB_KEY_XF86Launch0,             Key.Launch0},
        {XKB_KEY_XF86Launch1,             Key.Launch1},
        {XKB_KEY_XF86Launch2,             Key.Launch2},
        {XKB_KEY_XF86Launch3,             Key.Launch3},
        {XKB_KEY_XF86Launch4,             Key.Launch4},
        {XKB_KEY_XF86Launch5,             Key.Launch5},
        {XKB_KEY_XF86Launch6,             Key.Launch6},
        {XKB_KEY_XF86Launch7,             Key.Launch7},
        {XKB_KEY_XF86Launch8,             Key.Launch8},
        {XKB_KEY_XF86Launch9,             Key.Launch9},
        {XKB_KEY_XF86LaunchA,             Key.LaunchA},
        {XKB_KEY_XF86LaunchB,             Key.LaunchB},
        {XKB_KEY_XF86LaunchC,             Key.LaunchC},
        {XKB_KEY_XF86LaunchD,             Key.LaunchD},
        {XKB_KEY_XF86LaunchE,             Key.LaunchE},
        {XKB_KEY_XF86LaunchF,             Key.LaunchF}
        */
    };

    static bool isLatin1(uint sym) =>
        sym <= 0xff;

    internal static RawInputModifiers GetModifiers(xkb_state state)
    {
        RawInputModifiers modifiers = default;

        if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_CTRL, xkb_state_component.XKB_STATE_MODS_EFFECTIVE) > 0)
            modifiers |= RawInputModifiers.Control;
        if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_ALT, xkb_state_component.XKB_STATE_MODS_EFFECTIVE) > 0)
            modifiers |= RawInputModifiers.Alt;
        if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_SHIFT, xkb_state_component.XKB_STATE_MODS_EFFECTIVE) > 0)
            modifiers |= RawInputModifiers.Shift;
        if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_LOGO, xkb_state_component.XKB_STATE_MODS_EFFECTIVE) > 0)
            modifiers |= RawInputModifiers.Meta;

        return modifiers;
    }

    internal static uint lookupLatinKeysym(xkb_state state, uint keycode)
    {
        uint layout;
        uint sym = XKB_KEY_NoSymbol;
        using xkb_keymap keymap = xkb_state_get_keymap(state);
        var layoutCount = xkb_keymap_num_layouts_for_key(keymap, keycode);
        var currentLayout = xkb_state_key_get_layout(state, keycode);
        // Look at user layouts in the order in which they are defined in system
        // settings to find a latin keysym.
        for (layout = 0; layout < layoutCount; ++layout)
        {
            if (layout == currentLayout)
                continue;

            var level = xkb_state_key_get_level(state, keycode, layout);
            if (xkb_keymap_key_get_syms_by_level(keymap, keycode, layout, level, out var syms_p) != 1)
                continue;
            var syms = (uint)Marshal.ReadInt32(syms_p);
            if (isLatin1(syms))
            {
                sym = syms;
                break;
            }
        }

        if (sym == XKB_KEY_NoSymbol)
            return sym;

        var latchedMods = xkb_state_serialize_mods(state, xkb_state_component.XKB_STATE_MODS_LATCHED);
        var lockedMods = xkb_state_serialize_mods(state, xkb_state_component.XKB_STATE_MODS_LOCKED);

        // Check for uniqueness, consider the following setup:
        // setxkbmap -layout us,ru,us -variant dvorak,, -option 'grp:ctrl_alt_toggle' (set 'ru' as active).
        // In this setup, the user would expect to trigger a ctrl+q shortcut by pressing ctrl+<physical x key>,
        // because "US dvorak" is higher up in the layout settings list. This check verifies that an obtained
        // 'sym' can not be acquired by any other layout higher up in the user's layout list. If it can be acquired
        // then the obtained key is not unique. This prevents ctrl+<physical q key> from generating a ctrl+q
        // shortcut in the above described setup. We don't want ctrl+<physical x key> and ctrl+<physical q key> to
        // generate the same shortcut event in this case.
        var minKeycode = xkb_keymap_min_keycode(keymap);
        var maxKeycode = xkb_keymap_max_keycode(keymap);
        for (uint prevLayout = 0; prevLayout < layout; ++prevLayout)
        {
            {
                using var fs = xkb_state_new(keymap);
                xkb_state_update_mask(fs, 0, latchedMods, lockedMods, 0, 0, prevLayout);
            }
            for (uint code = minKeycode; code < maxKeycode; ++code)
            {
                {
                    using var fs = xkb_state_new(keymap);
                    var prevSym = xkb_state_key_get_one_sym(fs, code);
                    if (prevSym == sym)
                    {
                        sym = XKB_KEY_NoSymbol;
                        break;
                    }
                }
            }
        }

        return sym;
    }

    internal static Key KeysymToAvaloniaKey(uint keysym
        , RawInputModifiers modifiers
        , xkb_state state
        , uint code)
    {
        // Note 1: All standard key sequences on linux (as defined in platform theme)
        // that use a latin character also contain a control modifier, which is why
        // checking for Qt::ControlModifier is sufficient here. It is possible to
        // override QPlatformTheme::keyBindings() and provide custom sequences for
        // QKeySequence::StandardKey. Custom sequences probably should respect this
        // convention (alternatively, we could test against other modifiers here).
        // Note 2: The possibleKeys() shorcut mechanism is not affected by this value
        // adjustment and does its own thing.
        if (modifiers.HasFlag(RawInputModifiers.Control))
        {
            // With standard shortcuts we should prefer a latin character, this is
            // for checks like "some qkeyevent == QKeySequence::Copy" to work even
            // when using for example 'russian' keyboard layout.
            if (!isLatin1(keysym))
            {
                uint latinKeysym = lookupLatinKeysym(state, code);
                if (latinKeysym != XKB_KEY_NoSymbol)
                    keysym = latinKeysym;
            }
        }

        return keysymToQtKey_internal(keysym, modifiers, state, code, false, false);
    }

    static (uint lower, uint upper) qt_UCSConvertCase(char code)
        => (char.ToLower(code), char.ToUpper(code));

    static (uint lower, uint upper) xkbcommon_XConvertCase(uint sym)
    {
        uint lower, upper;
        /* Latin 1 keysym */
        if (sym < 0x100)
        {
            return qt_UCSConvertCase((char)sym);

        }

        /* Unicode keysym */
        if ((sym & 0xff000000) == 0x01000000)
        {
            (lower, upper) = qt_UCSConvertCase((char)(sym & 0x00ffffff));
            upper |= 0x01000000;
            lower |= 0x01000000;
            return (lower, upper);
        }

        /* Legacy keysym */

        lower = sym;
        upper = sym;

        switch (sym >> 8)
        {
            case 1: /* Latin 2 */
                /* Assume the KeySym is a legal value (ignore discontinuities) */
                if (sym == XKB_KEY_Aogonek)
                    lower = XKB_KEY_aogonek;
                else if (sym >= XKB_KEY_Lstroke && sym <= XKB_KEY_Sacute)
                    lower += (XKB_KEY_lstroke - XKB_KEY_Lstroke);
                else if (sym >= XKB_KEY_Scaron && sym <= XKB_KEY_Zacute)
                    lower += (XKB_KEY_scaron - XKB_KEY_Scaron);
                else if (sym >= XKB_KEY_Zcaron && sym <= XKB_KEY_Zabovedot)
                    lower += (XKB_KEY_zcaron - XKB_KEY_Zcaron);
                else if (sym == XKB_KEY_aogonek)
                    upper = XKB_KEY_Aogonek;
                else if (sym >= XKB_KEY_lstroke && sym <= XKB_KEY_sacute)
                    upper -= (XKB_KEY_lstroke - XKB_KEY_Lstroke);
                else if (sym >= XKB_KEY_scaron && sym <= XKB_KEY_zacute)
                    upper -= (XKB_KEY_scaron - XKB_KEY_Scaron);
                else if (sym >= XKB_KEY_zcaron && sym <= XKB_KEY_zabovedot)
                    upper -= (XKB_KEY_zcaron - XKB_KEY_Zcaron);
                else if (sym >= XKB_KEY_Racute && sym <= XKB_KEY_Tcedilla)
                    lower += (XKB_KEY_racute - XKB_KEY_Racute);
                else if (sym >= XKB_KEY_racute && sym <= XKB_KEY_tcedilla)
                    upper -= (XKB_KEY_racute - XKB_KEY_Racute);
                break;
            case 2: /* Latin 3 */
                /* Assume the KeySym is a legal value (ignore discontinuities) */
                if (sym >= XKB_KEY_Hstroke && sym <= XKB_KEY_Hcircumflex)
                    lower += (XKB_KEY_hstroke - XKB_KEY_Hstroke);
                else if (sym >= XKB_KEY_Gbreve && sym <= XKB_KEY_Jcircumflex)
                    lower += (XKB_KEY_gbreve - XKB_KEY_Gbreve);
                else if (sym >= XKB_KEY_hstroke && sym <= XKB_KEY_hcircumflex)
                    upper -= (XKB_KEY_hstroke - XKB_KEY_Hstroke);
                else if (sym >= XKB_KEY_gbreve && sym <= XKB_KEY_jcircumflex)
                    upper -= (XKB_KEY_gbreve - XKB_KEY_Gbreve);
                else if (sym >= XKB_KEY_Cabovedot && sym <= XKB_KEY_Scircumflex)
                    lower += (XKB_KEY_cabovedot - XKB_KEY_Cabovedot);
                else if (sym >= XKB_KEY_cabovedot && sym <= XKB_KEY_scircumflex)
                    upper -= (XKB_KEY_cabovedot - XKB_KEY_Cabovedot);
                break;
            case 3: /* Latin 4 */
                /* Assume the KeySym is a legal value (ignore discontinuities) */
                if (sym >= XKB_KEY_Rcedilla && sym <= XKB_KEY_Tslash)
                    lower += (XKB_KEY_rcedilla - XKB_KEY_Rcedilla);
                else if (sym >= XKB_KEY_rcedilla && sym <= XKB_KEY_tslash)
                    upper -= (XKB_KEY_rcedilla - XKB_KEY_Rcedilla);
                else if (sym == XKB_KEY_ENG)
                    lower = XKB_KEY_eng;
                else if (sym == XKB_KEY_eng)
                    upper = XKB_KEY_ENG;
                else if (sym >= XKB_KEY_Amacron && sym <= XKB_KEY_Umacron)
                    lower += (XKB_KEY_amacron - XKB_KEY_Amacron);
                else if (sym >= XKB_KEY_amacron && sym <= XKB_KEY_umacron)
                    upper -= (XKB_KEY_amacron - XKB_KEY_Amacron);
                break;
            case 6: /* Cyrillic */
                /* Assume the KeySym is a legal value (ignore discontinuities) */
                if (sym >= XKB_KEY_Serbian_DJE && sym <= XKB_KEY_Serbian_DZE)
                    lower -= (XKB_KEY_Serbian_DJE - XKB_KEY_Serbian_dje);
                else if (sym >= XKB_KEY_Serbian_dje && sym <= XKB_KEY_Serbian_dze)
                    upper += (XKB_KEY_Serbian_DJE - XKB_KEY_Serbian_dje);
                else if (sym >= XKB_KEY_Cyrillic_YU && sym <= XKB_KEY_Cyrillic_HARDSIGN)
                    lower -= (XKB_KEY_Cyrillic_YU - XKB_KEY_Cyrillic_yu);
                else if (sym >= XKB_KEY_Cyrillic_yu && sym <= XKB_KEY_Cyrillic_hardsign)
                    upper += (XKB_KEY_Cyrillic_YU - XKB_KEY_Cyrillic_yu);
                break;
            case 7: /* Greek */
                /* Assume the KeySym is a legal value (ignore discontinuities) */
                if (sym >= XKB_KEY_Greek_ALPHAaccent && sym <= XKB_KEY_Greek_OMEGAaccent)
                    lower += (XKB_KEY_Greek_alphaaccent - XKB_KEY_Greek_ALPHAaccent);
                else if (sym >= XKB_KEY_Greek_alphaaccent && sym <= XKB_KEY_Greek_omegaaccent &&
                     sym != XKB_KEY_Greek_iotaaccentdieresis &&
                     sym != XKB_KEY_Greek_upsilonaccentdieresis)
                    upper -= (XKB_KEY_Greek_alphaaccent - XKB_KEY_Greek_ALPHAaccent);
                else if (sym >= XKB_KEY_Greek_ALPHA && sym <= XKB_KEY_Greek_OMEGA)
                    lower += (XKB_KEY_Greek_alpha - XKB_KEY_Greek_ALPHA);
                else if (sym >= XKB_KEY_Greek_alpha && sym <= XKB_KEY_Greek_omega &&
                     sym != XKB_KEY_Greek_finalsmallsigma)
                    upper -= (XKB_KEY_Greek_alpha - XKB_KEY_Greek_ALPHA);
                break;
            case 0x13: /* Latin 9 */
                if (sym == XKB_KEY_OE)
                    lower = XKB_KEY_oe;
                else if (sym == XKB_KEY_oe)
                    upper = XKB_KEY_OE;
                else if (sym == XKB_KEY_Ydiaeresis)
                    lower = XKB_KEY_ydiaeresis;
                break;
        }
        return (lower, upper);
    }


    static uint qxkbcommon_xkb_keysym_to_upper(uint ks)
    {
        return xkbcommon_XConvertCase(ks).upper;
    }

    static string lookupString(xkb_state state, uint code)
    {
        var size = xkb_state_key_get_utf8(state, code, null, 0);
        if (size == 0)
        {
            return string.Empty;
        }
        System.Text.StringBuilder buffer = new(size + 1);
        xkb_state_key_get_utf8(state, code, buffer, (uint)size + 1);
        return buffer.ToString();
    }

    static string lookupStringNoKeysymTransformations(uint keysym)
    {
        System.Text.StringBuilder buffer = new(32);
        var size = -1;
        while (size < 0)
        {
            size = xkb_keysym_to_utf8(keysym, buffer, (uint)buffer.Capacity);
            buffer.Capacity += 10;
        }
        if (size == 0)
            return string.Empty; // the keysym does not have a Unicode representation
        return buffer.ToString(0, size - 1);
    }

    static bool ContainsUnicodeDigitCharacter(string input)
    {
        const int MaxAnsiCode = 255;

        return input.Any(c => char.IsDigit(c) && c > MaxAnsiCode);
    }

    static Key keysymToQtKey_internal(uint keysym
        , RawInputModifiers modifiers
        , xkb_state state
        , uint code
        , bool superAsMeta
        , bool hyperAsMeta)
    {
        uint? qtKey = default;

        // lookup from direct mapping
        // Avalonia has not key F25-F35
        if (keysym >= XKB_KEY_F1 && keysym <= XKB_KEY_F24)
        {
            // function keys
            qtKey = F1 + (keysym - XKB_KEY_F1);
        }
        else if (keysym >= XKB_KEY_KP_0 && keysym <= XKB_KEY_KP_9)
        {
            // numeric keypad keys
            qtKey = (uint)Key.NumPad0 + (keysym - XKB_KEY_KP_0);
        }
        else if (isLatin1(keysym))
        {
            // Upper-case first, since Qt::Keys are defined in terms of their
            // upper-case versions.
            qtKey = qxkbcommon_xkb_keysym_to_upper(keysym);
            // Upper-casing a Latin1 character might move it out of Latin1 range,
            // for example U+00B5 MICRO SIGN, which upper-case equivalent is
            // U+039C GREEK CAPITAL LETTER MU. If that's the case, then map the
            // original lower-case character.
            if (!isLatin1(qtKey.Value))
                qtKey = keysym;
        }
        else
        {
            if (s_SysKeyToAvKeyMap.TryGetValue(keysym, out var k))
            {
                return k;
            }
        }

        // lookup from unicode
        string text;
        if (!state.IsInvalid || modifiers.HasFlag(RawInputModifiers.Control))
        {
            // Control modifier changes the text to ASCII control character, therefore we
            // can't use this text to map keysym to a qt key. We can use the same keysym
            // (it is not affectd by transformation) to obtain untransformed text. For details
            // see "Appendix A. Default Symbol Transformations" in the XKB specification.
            text = lookupStringNoKeysymTransformations(keysym);
        }
        else
        {
            text = lookupString(state, code);
        }
        if (!string.IsNullOrWhiteSpace(text))
        {
            var c = text[0];
            if (ContainsUnicodeDigitCharacter(text))
            {
                // Ensures that also non-latin digits are mapped to corresponding qt keys,
                // e.g CTRL + ۲ (arabic two), is mapped to CTRL + Qt::Key_2.
                uint.TryParse(text, out var digit);
                qtKey = (uint)Key.D0 + digit;
            }
            else
            {


                qtKey = (uint)char.ToUpper(text[0]) - 17;
            }
        }

        return (Key)qtKey;
    }

}
