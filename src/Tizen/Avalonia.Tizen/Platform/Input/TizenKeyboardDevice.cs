using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Tizen.NUI.Components;
using Tizen.Uix.InputMethod;
using static System.Net.Mime.MediaTypeNames;

namespace Avalonia.Tizen.Platform.Input;
internal class TizenKeyboardDevice : KeyboardDevice, IKeyboardDevice
{
    private static readonly Dictionary<string, Key> SpecialKeys = new Dictionary<string, Key>
    {
        // Media keys
        { "XF86Red", Key.MediaRed },
        { "XF86Green", Key.MediaGreen },
        { "XF86Yellow", Key.MediaYellow },
        { "XF86Blue", Key.MediaBlue },
        { "XF86Info", Key.MediaInfo },
        { "XF86SimpleMenu", Key.MediaMenu },
        { "XF86Caption", Key.MediaSubtitle },
        { "XF86MTS", Key.None },
        { "XF86PictureSize", Key.None },
        { "XF86More", Key.MediaMore },
        { "XF86Search", Key.MediaSearch },
        { "XF863D", Key.None },
        { "XF86AudioRewind", Key.MediaPreviousTrack },
        { "XF86AudioPause", Key.MediaPlayPause },
        { "XF86AudioNext", Key.MediaNextTrack },
        { "XF86AudioRecord", Key.MediaRecord },
        { "XF86AudioPlay", Key.MediaPlayPause },
        { "XF86AudioStop", Key.MediaStop },
        { "XF86ChannelGuide", Key.MediaTvGuide },
        { "XF86SysMenu", Key.Apps },
        { "minus", Key.OemMinus },
        { "XF86PreviousChannel", Key.MediaPreviousChannel },
        { "XF86AudioMute", Key.VolumeMute },
        { "XF86ChannelList", Key.MediaChannelList },
        { "XF86RaiseChannel", Key.MediaChannelRaise },
        { "XF86LowerChannel", Key.MediaChannelLower },
        { "XF86AudioRaiseVolume", Key.VolumeUp },
        { "XF86AudioLowerVolume", Key.VolumeDown },
        { "XF86Display", Key.None },
        { "XF86PowerOff", Key.Sleep },
        { "XF86PlayBack", Key.MediaPlayPause },
        { "XF86Home", Key.MediaHome },
        { "XF86Back", Key.Escape }, // Back button should be mapped as Esc
        { "XF86Exit", Key.Cancel },

        { "Shift_L", Key.LeftShift },
        { "Control_L", Key.LeftCtrl },
        { "Alt_L", Key.LeftAlt },
        { "Super_L", Key.LWin },
        { "Alt_R", Key.RightAlt },
        { "Control_R", Key.RightCtrl },
        { "Shift_R", Key.RightShift },
        { "Super_R", Key.RWin },
        { "Menu", Key.Apps },
        { "Tab", Key.Tab },
        { "BackSpace", Key.Back },
        { "Return", Key.Return },
        { "Delete", Key.Delete },
        { "End", Key.End },
        { "Next", Key.Next },
        { "Prior", Key.Prior },
        { "Home", Key.Home },
        { "Insert", Key.Insert },
        { "Num_Lock", Key.NumLock },
        { "Left", Key.Left },
        { "Up", Key.Up },
        { "Right", Key.Right },
        { "Down", Key.Down },
        { "Escape", Key.Escape },
        { "Caps_Lock", Key.CapsLock },
        { "Pause", Key.Pause },
        { "Scroll_Lock", Key.Scroll },
        { "Scroll", Key.Scroll },

    };

    internal static Key GetSpecialKey(string key)
    {
        return SpecialKeys.TryGetValue(key, out var result) ? result : Key.None;
    }

    internal static Key GetAsciiKey(char keyCode) => keyCode switch
    {
        '`' or '~' => Key.Oem7,
        '0' or ')' => Key.D0,
        '1' or '!' => Key.D1,
        '2' or '@' => Key.D2,
        '3' or '#' => Key.D3,
        '4' or '$' => Key.D4,
        '5' or '%' => Key.D5,
        '6' or '^' => Key.D6,
        '7' or '&' => Key.D7,
        '8' or '*' => Key.D8,
        '9' or '(' => Key.D9,
        '\'' or '"' => Key.OemQuotes,
        '-' or '_' => Key.OemMinus,
        '=' or '+' => Key.OemPlus,
        '<' or ',' => Key.OemComma,
        '>' or '.' => Key.OemPeriod,
        ';' or ':' => Key.OemSemicolon,
        '/' or '?' => Key.OemQuestion,
        '[' or '{' => Key.OemOpenBrackets,
        ']' or '}' => Key.OemCloseBrackets,
        '\\' or '|' => Key.OemPipe,
        'a' or 'A' => Key.A,
        'b' or 'B' => Key.B,
        'c' or 'C' => Key.C,
        'd' or 'D' => Key.D,
        'e' or 'E' => Key.E,
        'f' or 'F' => Key.F,
        'g' or 'G' => Key.G,
        'h' or 'H' => Key.H,
        'i' or 'I' => Key.I,
        'j' or 'J' => Key.J,
        'k' or 'K' => Key.K,
        'l' or 'L' => Key.L,
        'm' or 'M' => Key.M,
        'n' or 'N' => Key.N,
        'o' or 'O' => Key.O,
        'p' or 'P' => Key.P,
        'q' or 'Q' => Key.Q,
        'r' or 'R' => Key.R,
        's' or 'S' => Key.S,
        't' or 'T' => Key.T,
        'u' or 'U' => Key.U,
        'v' or 'V' => Key.V,
        'w' or 'W' => Key.W,
        'x' or 'X' => Key.X,
        'y' or 'Y' => Key.Y,
        'z' or 'Z' => Key.Z,
        _ => Key.None
    };
}
