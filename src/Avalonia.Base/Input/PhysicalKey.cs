#if AVALONIA_REMOTE_PROTOCOL
namespace Avalonia.Remote.Protocol.Input;
#else
namespace Avalonia.Input;
#endif

/// <summary>
/// Represents a keyboard physical key.<br/>
/// </summary>
/// <remarks>
/// The names follow the W3C codes: https://www.w3.org/TR/uievents-code/
/// </remarks>
public enum PhysicalKey
{
    /// <summary>
    /// Represents no key.
    /// </summary>
    None = 0,


    // ###################
    // Writing System Keys
    // ###################

    /// <summary>
    /// <c>`~</c> on a US keyboard.
    /// This is the <c>半角/全角/漢字</c> (hankaku/zenkaku/kanji) key on Japanese keyboards.
    /// </summary>
    Backquote = 1,

    /// <summary>
    /// Used for both the US <c>\|</c> (on the 101-key layout) and also for the key located between the <c>"</c> and
    /// <c>Enter</c> keys on row C of the 102-, 104- and 106-key layouts.
    /// <c>#~</c> on a UK (102) keyboard.
    /// </summary>
    Backslash = 2,

    /// <summary>
    /// <c>[{</c> on a US keyboard.
    /// </summary>
    BracketLeft = 3,

    /// <summary>
    /// <c>]}</c> on a US keyboard.
    /// </summary>
    BracketRight = 4,

    /// <summary>
    /// <c>,&lt;</c> on a US keyboard.
    /// </summary>
    Comma = 5,

    /// <summary>
    /// <c>0)</c> on a US keyboard.
    /// </summary>
    Digit0 = 6,

    /// <summary>
    /// <c>1!</c> on a US keyboard.
    /// </summary>
    Digit1 = 7,

    /// <summary>
    /// <c>2@</c> on a US keyboard.
    /// </summary>
    Digit2 = 8,

    /// <summary>
    /// <c>3#</c> on a US keyboard.
    /// </summary>
    Digit3 = 9,

    /// <summary>
    /// <c>4$</c> on a US keyboard.
    /// </summary>
    Digit4 = 10,

    /// <summary>
    /// <c>5%</c> on a US keyboard.
    /// </summary>
    Digit5 = 11,

    /// <summary>
    /// <c>6^</c> on a US keyboard.
    /// </summary>
    Digit6 = 12,

    /// <summary>
    /// <c>7&amp;</c> on a US keyboard.
    /// </summary>
    Digit7 = 13,

    /// <summary>
    /// <c>8*</c> on a US keyboard.
    /// </summary>
    Digit8 = 14,

    /// <summary>
    /// <c>9(</c> on a US keyboard.
    /// </summary>
    Digit9 = 15,

    /// <summary>
    /// <c>=+</c> on a US keyboard.
    /// </summary>
    Equal = 16,

    /// <summary>
    /// Located between the left <c>Shift</c> and <c>Z</c> keys.
    /// <c>\|</c> on a UK keyboard.
    /// </summary>
    IntlBackslash = 17,

    /// <summary>
    /// Located between the <c>/</c> and right <c>Shift</c> keys.
    /// <c>\ろ</c> (ro) on a Japanese keyboard.
    /// </summary>
    IntlRo = 18,

    /// <summary>
    /// Located between the <c>=</c> and <c>Backspace</c> keys.
    /// <c>¥</c> (yen) on a Japanese keyboard.
    /// <c>\/</c> on a Russian keyboard.
    /// </summary>
    IntlYen = 19,

    /// <summary>
    /// <c>a</c> on a US keyboard.
    /// <c>q</c> on an AZERTY (e.g., French) keyboard.
    /// </summary>
    A = 20,

    /// <summary>
    /// <c>b</c> on a US keyboard.
    /// </summary>
    B = 21,

    /// <summary>
    /// <c>c</c> on a US keyboard.
    /// </summary>
    C = 22,

    /// <summary>
    /// <c>d</c> on a US keyboard.
    /// </summary>
    D = 23,

    /// <summary>
    /// <c>e</c> on a US keyboard.
    /// </summary>
    E = 24,

    /// <summary>
    /// <c>f</c> on a US keyboard.
    /// </summary>
    F = 25,

    /// <summary>
    /// <c>g</c> on a US keyboard.
    /// </summary>
    G = 26,

    /// <summary>
    /// <c>h</c> on a US keyboard.
    /// </summary>
    H = 27,

    /// <summary>
    /// <c>i</c> on a US keyboard.
    /// </summary>
    I = 28,

    /// <summary>
    /// <c>j</c> on a US keyboard.
    /// </summary>
    J = 29,

    /// <summary>
    /// <c>k</c> on a US keyboard.
    /// </summary>
    K = 30,

    /// <summary>
    /// <c>l</c> on a US keyboard.
    /// </summary>
    L = 31,

    /// <summary>
    /// <c>m</c> on a US keyboard.
    /// </summary>
    M = 32,

    /// <summary>
    /// <c>n</c> on a US keyboard.
    /// </summary>
    N = 33,

    /// <summary>
    /// <c>o</c> on a US keyboard.
    /// </summary>
    O = 34,

    /// <summary>
    /// <c>p</c> on a US keyboard.
    /// </summary>
    P = 35,

    /// <summary>
    /// <c>q</c> on a US keyboard.
    /// <c>a</c> on an AZERTY (e.g., French) keyboard.
    /// </summary>
    Q = 36,

    /// <summary>
    /// <c>r</c> on a US keyboard.
    /// </summary>
    R = 37,

    /// <summary>
    /// <c>s</c> on a US keyboard.
    /// </summary>
    S = 38,

    /// <summary>
    /// <c>t</c> on a US keyboard.
    /// </summary>
    T = 39,

    /// <summary>
    /// <c>u</c> on a US keyboard.
    /// </summary>
    U = 40,

    /// <summary>
    /// <c>v</c> on a US keyboard.
    /// </summary>
    V = 41,

    /// <summary>
    /// <c>w</c> on a US keyboard.
    /// <c>z</c> on an AZERTY (e.g., French) keyboard.
    /// </summary>
    W = 42,

    /// <summary>
    /// <c>x</c> on a US keyboard.
    /// </summary>
    X = 43,

    /// <summary>
    /// <c>y</c> on a US keyboard.
    /// <c>z</c> on a QWERTZ (e.g., German) keyboard.
    /// </summary>
    Y = 44,

    /// <summary>
    /// <c>z</c> on a US keyboard.
    /// <c>w</c> on an AZERTY (e.g., French) keyboard.
    /// <c>y</c> on a QWERTZ (e.g., German) keyboard.
    /// </summary>
    Z = 45,

    /// <summary>
    /// <c>-_</c> on a US keyboard.
    /// </summary>
    Minus = 46,

    /// <summary>
    /// <c>.&gt;</c> on a US keyboard.
    /// </summary>
    Period = 47,

    /// <summary>
    /// <c>'"</c> on a US keyboard.
    /// </summary>
    Quote = 48,

    /// <summary>
    /// <c>;:</c> on a US keyboard.
    /// </summary>
    Semicolon = 49,

    /// <summary>
    /// <c>/?</c> on a US keyboard.
    /// </summary>
    Slash = 50,


    // ###############
    // Functional keys
    // ###############

    /// <summary>
    /// <c>Alt</c>, <c>Option</c> or <c>⌥</c>.
    /// </summary>
    AltLeft = 51,

    /// <summary>
    /// <c>Alt</c>, <c>Option</c> or <c>⌥</c>.
    /// This is labelled <c>AltGr</c> key on many keyboard layouts.
    /// </summary>
    AltRight = 52,

    /// <summary>
    /// <c>Backspace</c> or <c>⌫</c>.
    /// Labelled <c>Delete</c> on Apple keyboards.
    /// </summary>
    Backspace = 53,

    /// <summary>
    /// <c>CapsLock</c> or <c>⇪</c>.
    /// </summary>
    CapsLock = 54,

    /// <summary>
    /// The application context menu key, which is typically found between the right <c>Meta</c> key
    /// and the right <c>Control</c> key.
    /// </summary>
    ContextMenu = 55,

    /// <summary>
    /// <c>Control</c> or <c>⌃</c>.
    /// </summary>
    ControlLeft = 56,

    /// <summary>
    /// <c>Control</c> or <c>⌃</c>.
    /// </summary>
    ControlRight = 57,

    /// <summary>
    /// <c>Enter</c> or <c>↵</c>.
    /// Labelled <c>Return</c> on Apple keyboards.
    /// </summary>
    Enter = 58,

    /// <summary>
    /// The <c>⊞</c> (Windows), <c>⌘</c>, <c>Command</c> or other OS symbol key.
    /// </summary>
    MetaLeft = 59,

    /// <summary>
    /// The <c>⊞</c> (Windows), <c>⌘</c>, <c>Command</c> or other OS symbol key.
    /// </summary>
    MetaRight = 60,

    /// <summary>
    /// <c>Shift</c> or <c>⇧</c>.
    /// </summary>
    ShiftLeft = 61,

    /// <summary>
    /// <c>Shift</c> or <c>⇧</c>.
    /// </summary>
    ShiftRight = 62,

    /// <summary>
    /// <c> </c> (space).
    /// </summary>
    Space = 63,

    /// <summary>
    /// <c>Tab</c> or <c>⇥</c>.
    /// </summary>
    Tab = 64,

    /// <summary>
    /// Japanese: <c>変換</c> (henkan).
    /// </summary>
    Convert = 65,

    /// <summary>
    /// Japanese: <c>カタカナ/ひらがな/ローマ字</c> (katakana/hiragana/romaji).
    /// </summary>
    KanaMode = 66,

    /// <summary>
    /// Korean: HangulMode <c>한/영</c> (han/yeong).
    /// Japanese (Mac keyboard): <c>かな</c> (kana).
    /// </summary>
    Lang1 = 67,

    /// <summary>
    /// Korean: Hanja <c>한자</c> (hanja).
    /// Japanese (Mac keyboard): <c>英数</c> (eisu).
    /// </summary>
    Lang2 = 68,

    /// <summary>
    /// Japanese (word-processing keyboard): Katakana.
    /// </summary>
    Lang3 = 69,

    /// <summary>
    /// Japanese (word-processing keyboard): Hiragana.
    /// </summary>
    Lang4 = 70,

    /// <summary>
    /// Japanese (word-processing keyboard): Zenkaku/Hankaku.
    /// </summary>
    Lang5 = 71,

    /// <summary>
    /// Japanese: <c>無変換</c> (muhenkan).
    /// </summary>
    NonConvert = 72,


    // ###################
    // Control Pad Section
    // ###################

    /// <summary>
    ///	<c>⌦</c>. The forward delete key.
    /// Note that on Apple keyboards, the key labelled <c>Delete</c> on the main part of the keyboard is
    /// <see cref="Backspace"/>.
    /// </summary>
    Delete = 73,

    /// <summary>
    /// <c>End</c> or <c>↘</c>.
    /// </summary>
    End = 74,

    /// <summary>
    /// <c>Help</c>.
    /// Not present on standard PC keyboards.
    /// </summary>
    Help = 75,

    /// <summary>
    /// <c>Home</c> or <c>↖</c>.
    /// </summary>
    Home = 76,

    /// <summary>
    /// <c>Insert</c> or <c>Ins</c>.
    /// Not present on Apple keyboards.
    /// </summary>
    Insert = 77,

    /// <summary>
    /// <c>Page Down</c>, <c>PgDn</c> or <c>⇟</c>.
    /// </summary>
    PageDown = 78,

    /// <summary>
    /// <c>Page Up</c>, <c>PgUp</c> or <c>⇞</c>.
    /// </summary>
    PageUp = 79,


    // #################
    // Arrow Pad Section
    // #################

    /// <summary>
    /// <c>↓</c>.
    /// </summary>
    ArrowDown = 80,

    /// <summary>
    /// <c>←</c>.
    /// </summary>
    ArrowLeft = 81,

    /// <summary>
    /// <c>→</c>.
    /// </summary>
    ArrowRight = 82,

    /// <summary>
    /// <c>↑</c>.
    /// </summary>
    ArrowUp = 83,


    // ######################
    // Numeric Keypad Section
    // ######################

    /// <summary>
    /// Numeric keypad <c>Num Lock</c>.
    /// On the Mac, this is used for the numpad <c>Clear</c> key.
    /// </summary>
    NumLock = 84,

    /// <summary>
    /// Numeric keypad <c>0 Ins</c> on a keyboard.
    /// <c>0</c> on a phone or remote control.
    /// </summary>
    NumPad0 = 85,

    /// <summary>
    /// Numeric keypad <c>1 End</c> on a keyboard.
    /// <c>1</c> or <c>1 QZ</c> on a phone or remote control.
    /// </summary>
    NumPad1 = 86,

    /// <summary>
    /// Numeric keypad <c>2 ↓</c> on a keyboard.
    /// <c>2 ABC</c> on a phone or remote control.
    /// </summary>
    NumPad2 = 87,

    /// <summary>
    /// Numeric keypad <c>3 PgDn</c> on a keyboard.
    /// <c>3 DEF</c> on a phone or remote control.
    /// </summary>
    NumPad3 = 88,

    /// <summary>
    /// Numeric keypad <c>4 ←</c> on a keyboard.
    /// <c>4 GHI</c> on a phone or remote control.
    /// </summary>
    NumPad4 = 89,

    /// <summary>
    /// Numeric keypad <c>5</c> on a keyboard.
    /// <c>5 JKL</c> on a phone or remote control.
    /// </summary>
    NumPad5 = 90,

    /// <summary>
    /// Numeric keypad <c>6 →</c> on a keyboard.
    /// <c>6 MNO</c> on a phone or remote control.
    /// </summary>
    NumPad6 = 91,

    /// <summary>
    /// Numeric keypad <c>7 Home</c> on a keyboard.
    /// <c>7 PQRS</c> or <c>7 PRS</c> on a phone or remote control.
    /// </summary>
    NumPad7 = 92,

    /// <summary>
    /// Numeric keypad <c>8 ↑</c> on a keyboard.
    /// <c>8 TUV</c> on a phone or remote control.
    /// </summary>
    NumPad8 = 93,

    /// <summary>
    /// Numeric keypad <c>9 PgUp</c> on a keyboard.
    /// <c>9 WXYZ</c> or <c>9 WXY</c> on a phone or remote control.
    /// </summary>
    NumPad9 = 94,

    /// <summary>
    /// Numeric keypad <c>+</c>.
    /// </summary>
    NumPadAdd = 95,

    /// <summary>
    /// Numeric keypad <c>C</c> or <c>AC</c> (All Clear).
    /// Also for use with numpads that have a <c>Clear</c> key that is separate from the <c>NumLock</c> key.
    /// On the Mac, the numpad <c>Clear</c> key is <see cref="NumLock"/>.
    /// </summary>
    NumPadClear = 96,

    /// <summary>
    /// Numeric keypad <c>,</c> (thousands separator).
    /// For locales where the thousands separator is a "." (e.g., Brazil), this key may generate a <c>.</c>.
    /// </summary>
    NumPadComma = 97,

    /// <summary>
    /// Numeric keypad <c>. Del</c>.
    /// For locales where the decimal separator is "," (e.g., Brazil), this key may generate a <c>,</c>.
    /// </summary>
    NumPadDecimal = 98,

    /// <summary>
    /// Numeric keypad <c>/</c>.
    /// </summary>
    NumPadDivide = 99,

    /// <summary>
    /// Numeric keypad <c>Enter</c>.
    /// </summary>
    NumPadEnter = 100,

    /// <summary>
    /// Numeric keypad <c>=</c>.
    /// </summary>
    NumPadEqual = 101,

    /// <summary>
    /// Numeric keypad <c>*</c> on a keyboard.
    /// For use with numpads that provide mathematical operations (<c>+</c>, <c>-</c>, <c>*</c> and <c>/</c>).
    /// </summary>
    NumPadMultiply = 102,

    /// <summary>
    /// Numeric keypad <c>(</c>.
    /// Found on the Microsoft Natural Keyboard.
    /// </summary>
    NumPadParenLeft = 103,

    /// <summary>
    /// Numeric keypad <c>)</c>.
    /// Found on the Microsoft Natural Keyboard.
    /// </summary>
    NumPadParenRight = 104,

    /// <summary>
    /// Numeric keypad <c>-</c>.
    /// </summary>
    NumPadSubtract = 105,


    // ################
    // Function Section
    // ################

    /// <summary>
    /// <c>Esc</c> or <c>⎋</c>.
    /// </summary>
    Escape = 106,

    /// <summary>
    /// <c>F1</c>.
    /// </summary>
    F1 = 107,

    /// <summary>
    /// <c>F2</c>.
    /// </summary>
    F2 = 108,

    /// <summary>
    /// <c>F3</c>.
    /// </summary>
    F3 = 109,

    /// <summary>
    /// <c>F4</c>.
    /// </summary>
    F4 = 110,

    /// <summary>
    /// <c>F5</c>.
    /// </summary>
    F5 = 111,

    /// <summary>
    /// <c>F6</c>.
    /// </summary>
    F6 = 112,

    /// <summary>
    /// <c>F7</c>.
    /// </summary>
    F7 = 113,

    /// <summary>
    /// <c>F8</c>.
    /// </summary>
    F8 = 114,

    /// <summary>
    /// <c>F9</c>.
    /// </summary>
    F9 = 115,

    /// <summary>
    /// <c>F10</c>.
    /// </summary>
    F10 = 116,

    /// <summary>
    /// <c>F11</c>.
    /// </summary>
    F11 = 117,

    /// <summary>
    /// <c>F12</c>.
    /// </summary>
    F12 = 118,

    /// <summary>
    /// <c>F13</c>.
    /// </summary>
    F13 = 119,

    /// <summary>
    /// <c>F14</c>.
    /// </summary>
    F14 = 120,

    /// <summary>
    /// <c>F15</c>.
    /// </summary>
    F15 = 121,

    /// <summary>
    /// <c>F16</c>.
    /// </summary>
    F16 = 122,

    /// <summary>
    /// <c>F17</c>.
    /// </summary>
    F17 = 123,

    /// <summary>
    /// <c>F18</c>.
    /// </summary>
    F18 = 124,

    /// <summary>
    /// <c>F19</c>.
    /// </summary>
    F19 = 125,

    /// <summary>
    /// <c>F20</c>.
    /// </summary>
    F20 = 126,

    /// <summary>
    /// <c>F21</c>.
    /// </summary>
    F21 = 127,

    /// <summary>
    /// <c>F22</c>.
    /// </summary>
    F22 = 128,

    /// <summary>
    /// <c>F23</c>.
    /// </summary>
    F23 = 129,

    /// <summary>
    /// <c>F24</c>.
    /// </summary>
    F24 = 130,

    /// <summary>
    /// <c>PrtScr SysRq</c> or <c>Print Screen</c>.
    /// </summary>
    PrintScreen = 131,

    /// <summary>
    /// <c>Scroll Lock</c>.
    /// </summary>
    ScrollLock = 132,

    /// <summary>
    /// <c>Pause Break</c>.
    /// </summary>
    Pause = 133,


    // ##########
    // Media Keys
    // ##########

    /// <summary>
    /// Browser <c>Back</c>.
    /// Some laptops place this key to the left of the <c>↑</c> key.
    /// </summary>
    BrowserBack = 134,

    /// <summary>
    /// Browser <c>Favorites</c>.
    /// </summary>
    BrowserFavorites = 135,

    /// <summary>
    /// Browser <c>Forward</c>.
    /// Some laptops place this key to the right of the <c>↑</c> key.
    /// </summary>
    BrowserForward = 136,

    /// <summary>
    /// Browser <c>Home</c>.
    /// </summary>
    BrowserHome = 137,

    /// <summary>
    /// Browser <c>Refresh</c>.
    /// </summary>
    BrowserRefresh = 138,

    /// <summary>
    /// Browser <c>Search</c>.
    /// </summary>
    BrowserSearch = 139,
    
    /// <summary>
    /// Browser <c>Stop</c>.
    /// </summary>
    BrowserStop = 140,
    
    /// <summary>
    /// <c>Eject</c> or <c>⏏</c>.
    /// This key is placed in the function section on some Apple keyboards.
    /// </summary>
    Eject = 141,

    /// <summary>
    /// <c>App 1</c>.
    /// Sometimes labelled <c>My Computer</c> on the keyboard.
    /// </summary>
    LaunchApp1 = 142,

    /// <summary>
    /// <c>App 2</c>.
    /// Sometimes labelled <c>Calculator</c> on the keyboard.
    /// </summary>
    LaunchApp2 = 143,

    /// <summary>
    /// <c>Mail</c>.
    /// </summary>
    LaunchMail = 144,

    /// <summary>
    /// Media <c>Play/Pause</c> or <c>⏵⏸</c>.
    /// </summary>
    MediaPlayPause = 145,

    /// <summary>
    /// Media <c>Select</c>.
    /// </summary>
    MediaSelect = 146,

    /// <summary>
    /// Media <c>Stop</c> or <c>⏹</c>.
    /// </summary>
    MediaStop = 147,

    /// <summary>
    /// Media <c>Next</c> or <c>⏭</c>.
    /// </summary>
    MediaTrackNext = 148,

    /// <summary>
    /// Media <c>Previous</c> or <c>⏮</c>.
    /// </summary>
    MediaTrackPrevious = 149,

    /// <summary>
    /// <c>Power</c>.
    /// </summary>
    Power = 150,

    /// <summary>
    /// <c>Sleep</c>.
    /// </summary>
    Sleep = 151,

    /// <summary>
    /// <c>Volume Down</c>.
    /// </summary>
    AudioVolumeDown = 152,

    /// <summary>
    /// <c>Mute</c>.
    /// </summary>
    AudioVolumeMute = 153,

    /// <summary>
    /// <c>Volume Up</c>.
    /// </summary>
    AudioVolumeUp = 154,

    /// <summary>
    /// <c>Wake Up</c>.
    /// </summary>
    WakeUp = 155,


    // ###########
    // Legacy Keys
    // ###########

    /// <summary>
    /// <c>Again</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Again = 156,

    /// <summary>
    /// <c>Copy</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Copy = 157,

    /// <summary>
    /// <c>Cut</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Cut = 158,

    /// <summary>
    /// <c>Find</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Find = 159,

    /// <summary>
    /// <c>Open</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Open = 160,

    /// <summary>
    /// <c>Paste</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Paste = 161,

    /// <summary>
    /// <c>Props</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Props = 162,

    /// <summary>
    /// <c>Select</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Select = 163,

    /// <summary>
    /// <c>Undo</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Undo = 164

}
