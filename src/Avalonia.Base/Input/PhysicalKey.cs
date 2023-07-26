namespace Avalonia.Input;

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
    /// <c>7&</c> on a US keyboard.
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
    KeyA = 20,

    /// <summary>
    /// <c>b</c> on a US keyboard.
    /// </summary>
    KeyB = 21,

    /// <summary>
    /// <c>c</c> on a US keyboard.
    /// </summary>
    KeyC = 22,

    /// <summary>
    /// <c>d</c> on a US keyboard.
    /// </summary>
    KeyD = 23,

    /// <summary>
    /// <c>e</c> on a US keyboard.
    /// </summary>
    KeyE = 24,

    /// <summary>
    /// <c>f</c> on a US keyboard.
    /// </summary>
    KeyF = 25,

    /// <summary>
    /// <c>g</c> on a US keyboard.
    /// </summary>
    KeyG = 26,

    /// <summary>
    /// <c>h</c> on a US keyboard.
    /// </summary>
    KeyH = 27,

    /// <summary>
    /// <c>i</c> on a US keyboard.
    /// </summary>
    KeyI = 28,

    /// <summary>
    /// <c>j</c> on a US keyboard.
    /// </summary>
    KeyJ = 29,

    /// <summary>
    /// <c>k</c> on a US keyboard.
    /// </summary>
    KeyK = 30,

    /// <summary>
    /// <c>l</c> on a US keyboard.
    /// </summary>
    KeyL = 31,

    /// <summary>
    /// <c>m</c> on a US keyboard.
    /// </summary>
    KeyM = 32,

    /// <summary>
    /// <c>n</c> on a US keyboard.
    /// </summary>
    KeyN = 33,

    /// <summary>
    /// <c>o</c> on a US keyboard.
    /// </summary>
    KeyO = 34,

    /// <summary>
    /// <c>p</c> on a US keyboard.
    /// </summary>
    KeyP = 35,

    /// <summary>
    /// <c>q</c> on a US keyboard.
    /// <c>a</c> on an AZERTY (e.g., French) keyboard.
    /// </summary>
    KeyQ = 36,

    /// <summary>
    /// <c>r</c> on a US keyboard.
    /// </summary>
    KeyR = 37,

    /// <summary>
    /// <c>s</c> on a US keyboard.
    /// </summary>
    KeyS = 38,

    /// <summary>
    /// <c>t</c> on a US keyboard.
    /// </summary>
    KeyT = 39,

    /// <summary>
    /// <c>u</c> on a US keyboard.
    /// </summary>
    KeyU = 40,

    /// <summary>
    /// <c>v</c> on a US keyboard.
    /// </summary>
    KeyV = 41,

    /// <summary>
    /// <c>w</c> on a US keyboard.
    /// <c>z</c> on an AZERTY (e.g., French) keyboard.
    /// </summary>
    KeyW = 42,

    /// <summary>
    /// <c>x</c> on a US keyboard.
    /// </summary>
    KeyX = 43,

    /// <summary>
    /// <c>y</c> on a US keyboard.
    /// <c>z</c> on a QWERTZ (e.g., German) keyboard.
    /// </summary>
    KeyY = 44,

    /// <summary>
    /// <c>z</c> on a US keyboard.
    /// <c>w</c> on an AZERTY (e.g., French) keyboard.
    /// <c>y</c> on a QWERTZ (e.g., German) keyboard.
    /// </summary>
    KeyZ = 45,

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


    // ##############
    // Numpad Section
    // ##############

    /// <summary>
    /// Numpad <c>Num Lock</c>.
    /// On the Mac, this is used for the numpad <c>Clear</c> key.
    /// </summary>
    NumLock = 84,

    /// <summary>
    /// Numpad <c>0 Ins</c> on a keyboard.
    /// <c>0</c> on a phone or remote control.
    /// </summary>
    Numpad0 = 85,

    /// <summary>
    /// Numpad <c>1 End</c> on a keyboard.
    /// <c>1</c> or <c>1 QZ</c> on a phone or remote control.
    /// </summary>
    Numpad1 = 86,

    /// <summary>
    /// Numpad <c>2 ↓</c> on a keyboard.
    /// <c>2 ABC</c> on a phone or remote control.
    /// </summary>
    Numpad2 = 87,

    /// <summary>
    /// Numpad <c>3 PgDn</c> on a keyboard.
    /// <c>3 DEF</c> on a phone or remote control.
    /// </summary>
    Numpad3 = 88,

    /// <summary>
    /// Numpad <c>4 ←</c> on a keyboard.
    /// <c>4 GHI</c> on a phone or remote control.
    /// </summary>
    Numpad4 = 89,

    /// <summary>
    /// Numpad <c>5</c> on a keyboard.
    /// <c>5 JKL</c> on a phone or remote control.
    /// </summary>
    Numpad5 = 90,

    /// <summary>
    /// Numpad <c>6 →</c> on a keyboard.
    /// <c>6 MNO</c> on a phone or remote control.
    /// </summary>
    Numpad6 = 91,

    /// <summary>
    /// Numpad <c>7 Home</c> on a keyboard.
    /// <c>7 PQRS</c> or <c>7 PRS</c> on a phone or remote control.
    /// </summary>
    Numpad7 = 92,

    /// <summary>
    /// Numpad <c>8 ↑</c> on a keyboard.
    /// <c>8 TUV</c> on a phone or remote control.
    /// </summary>
    Numpad8 = 93,

    /// <summary>
    /// Numpad <c>9 PgUp</c> on a keyboard.
    /// <c>9 WXYZ</c> or <c>9 WXY</c> on a phone or remote control.
    /// </summary>
    Numpad9 = 94,

    /// <summary>
    /// Numpad <c>+</c>.
    /// </summary>
    NumpadAdd = 95,

    /// <summary>
    /// Numpad <c>Backspace</c>.
    /// Found on the Microsoft Natural Keyboard.
    /// </summary>
    NumpadBackspace = 96,

    /// <summary>
    /// Numpad <c>C</c> or <c>AC</c> (All Clear).
    /// Also for use with numpads that have a <c>Clear</c> key that is separate from the <c>NumLock</c> key.
    /// On the Mac, the numpad <c>Clear</c> key is <see cref="NumLock"/>.
    /// </summary>
    NumpadClear = 97,

    /// <summary>
    /// Numpad <c>CE</c> (Clear Entry).
    /// </summary>
    NumpadClearEntry = 98,

    /// <summary>
    /// Numpad <c>,</c> (thousands separator).
    /// For locales where the thousands separator is a "." (e.g., Brazil), this key may generate a <c>.</c>.
    /// </summary>
    NumpadComma = 99,

    /// <summary>
    /// Numpad <c>. Del</c>.
    /// For locales where the decimal separator is "," (e.g., Brazil), this key may generate a <c>,</c>.
    /// </summary>
    NumpadDecimal = 100,

    /// <summary>
    /// Numpad <c>/</c>.
    /// </summary>
    NumpadDivide = 101,

    /// <summary>
    /// Numpad <c>Enter</c>.
    /// </summary>
    NumpadEnter = 102,

    /// <summary>
    /// Numpad <c>=</c>.
    /// </summary>
    NumpadEqual = 103,

    /// <summary>
    /// Numpad <c>#</c> on a phone or remote control device.
    /// This key is typically found below the <c>9</c> key and to the right of the <c>0</c> key.
    /// </summary>
    NumpadHash = 104,

    /// <summary>
    /// Numpad <c>M+</c>.
    /// Add current entry to the value stored in memory.
    /// </summary>
    NumpadMemoryAdd = 105,

    /// <summary>
    /// Numpad <c>MC</c>.
    /// Clear the value stored in memory.
    /// </summary>
    NumpadMemoryClear = 106,

    /// <summary>
    /// Numpad <c>MR</c>.
    /// Replace the current entry with the value stored in memory.
    /// </summary>
    NumpadMemoryRecall = 107,

    /// <summary>
    /// Numpad <c>MS</c>.
    /// Replace the value stored in memory with the current entry.
    /// </summary>
    NumpadMemoryStore = 108,

    /// <summary>
    /// Numpad <c>M-</c>.
    /// Subtract current entry from the value stored in memory.
    /// </summary>
    NumpadMemorySubtract = 109,

    /// <summary>
    /// Numpad <c>*</c> on a keyboard.
    /// For use with numpads that provide mathematical operations (<c>+</c>, <c>-</c>, <c>*</c> and <c>/</c>).
    /// Use <see cref="NumpadStar"/> for the <c>*</c> key on phones and remote controls.
    /// </summary>
    NumpadMultiply = 110,

    /// <summary>
    /// Numpad <c>(</c>.
    /// Found on the Microsoft Natural Keyboard.
    /// </summary>
    NumpadParenLeft = 111,

    /// <summary>
    /// Numpad <c>)</c>.
    /// Found on the Microsoft Natural Keyboard.
    /// </summary>
    NumpadParenRight = 112,

    /// <summary>
    /// <c>*</c> on a phone or remote control device.
    /// This key is typically found below the <c>7</c> key and to the left of the <c>0</c> key.
    /// Use <see cref="NumpadMultiply"/> for the <c>*</c> key on numeric keypads.
    /// </summary>
    NumpadStar = 113,

    /// <summary>
    /// Numpad <c>-</c>.
    /// </summary>
    NumpadSubtract = 114,


    // ################
    // Function Section
    // ################

    /// <summary>
    /// <c>Esc</c> or <c>⎋</c>.
    /// </summary>
    Escape = 115,

    /// <summary>
    /// <c>F1</c>.
    /// </summary>
    F1 = 116,

    /// <summary>
    /// <c>F2</c>.
    /// </summary>
    F2 = 117,

    /// <summary>
    /// <c>F3</c>.
    /// </summary>
    F3 = 118,

    /// <summary>
    /// <c>F4</c>.
    /// </summary>
    F4 = 119,

    /// <summary>
    /// <c>F5</c>.
    /// </summary>
    F5 = 120,

    /// <summary>
    /// <c>F6</c>.
    /// </summary>
    F6 = 121,

    /// <summary>
    /// <c>F7</c>.
    /// </summary>
    F7 = 122,

    /// <summary>
    /// <c>F8</c>.
    /// </summary>
    F8 = 123,

    /// <summary>
    /// <c>F9</c>.
    /// </summary>
    F9 = 124,

    /// <summary>
    /// <c>F10</c>.
    /// </summary>
    F10 = 125,

    /// <summary>
    /// <c>F11</c>.
    /// </summary>
    F11 = 126,

    /// <summary>
    /// <c>F12</c>.
    /// </summary>
    F12 = 127,

    /// <summary>
    /// <c>F13</c>.
    /// </summary>
    F13 = 128,

    /// <summary>
    /// <c>F14</c>.
    /// </summary>
    F14 = 129,

    /// <summary>
    /// <c>F15</c>.
    /// </summary>
    F15 = 130,

    /// <summary>
    /// <c>F16</c>.
    /// </summary>
    F16 = 131,

    /// <summary>
    /// <c>F17</c>.
    /// </summary>
    F17 = 132,

    /// <summary>
    /// <c>F18</c>.
    /// </summary>
    F18 = 133,

    /// <summary>
    /// <c>F19</c>.
    /// </summary>
    F19 = 134,

    /// <summary>
    /// <c>F20</c>.
    /// </summary>
    F20 = 135,

    /// <summary>
    /// <c>F21</c>.
    /// </summary>
    F21 = 136,

    /// <summary>
    /// <c>F22</c>.
    /// </summary>
    F22 = 137,

    /// <summary>
    /// <c>F23</c>.
    /// </summary>
    F23 = 138,

    /// <summary>
    /// <c>F24</c>.
    /// </summary>
    F24 = 139,

    /// <summary>
    /// <c>Fn</c>.
    /// This is typically a hardware key that does not generate a separate code.
    /// Most keyboards do not place this key in the function section.
    /// </summary>
    Fn = 140,

    /// <summary>
    /// <c>FLock</c> or <c>FnLock</c>.
    /// Function Lock key.
    /// Found on the Microsoft Natural Keyboard.
    /// </summary>
    FnLock = 141,

    /// <summary>
    /// <c>PrtScr SysRq</c> or <c>Print Screen</c>.
    /// </summary>
    PrintScreen = 142,

    /// <summary>
    /// <c>Scroll Lock</c>.
    /// </summary>
    ScrollLock = 143,

    /// <summary>
    /// <c>Pause Break</c>.
    /// </summary>
    Pause = 144,


    // ##########
    // Media Keys
    // ##########

    /// <summary>
    /// Browser <c>Back</c>.
    /// Some laptops place this key to the left of the <c>↑</c> key.
    /// </summary>
    BrowserBack = 145,

    /// <summary>
    /// Browser <c>Favorites</c>.
    /// </summary>
    BrowserFavorites = 146,

    /// <summary>
    /// Browser <c>Forward</c>.
    /// Some laptops place this key to the right of the <c>↑</c> key.
    /// </summary>
    BrowserForward = 147,

    /// <summary>
    /// Browser <c>Home</c>.
    /// </summary>
    BrowserHome = 148,

    /// <summary>
    /// Browser <c>Refresh</c>.
    /// </summary>
    BrowserRefresh = 149,

    /// <summary>
    /// Browser <c>Search</c>.
    /// </summary>
    BrowserSearch = 150,
    
    /// <summary>
    /// Browser <c>Stop</c>.
    /// </summary>
    BrowserStop = 151,
    
    /// <summary>
    /// <c>Eject</c> or <c>⏏</c>.
    /// This key is placed in the function section on some Apple keyboards.
    /// </summary>
    Eject = 152,

    /// <summary>
    /// <c>App 1</c>.
    /// Sometimes labelled <c>My Computer</c> on the keyboard.
    /// </summary>
    LaunchApp1 = 153,

    /// <summary>
    /// <c>App 2</c>.
    /// Sometimes labelled <c>Calculator</c> on the keyboard.
    /// </summary>
    LaunchApp2 = 154,

    /// <summary>
    /// <c>Mail</c>.
    /// </summary>
    LaunchMail = 155,

    /// <summary>
    /// Media <c>Play/Pause</c> or <c>⏵⏸</c>.
    /// </summary>
    MediaPlayPause = 156,

    /// <summary>
    /// Media <c>Select</c>.
    /// </summary>
    MediaSelect = 157,

    /// <summary>
    /// Media <c>Stop</c> or <c>⏹</c>.
    /// </summary>
    MediaStop = 158,

    /// <summary>
    /// Media <c>Next</c> or <c>⏭</c>.
    /// </summary>
    MediaTrackNext = 159,

    /// <summary>
    /// Media <c>Previous</c> or <c>⏮</c>.
    /// </summary>
    MediaTrackPrevious = 160,

    /// <summary>
    /// <c>Power</c>.
    /// </summary>
    Power = 161,

    /// <summary>
    /// <c>Sleep</c>.
    /// </summary>
    Sleep = 162,

    /// <summary>
    /// <c>Volume Down</c>.
    /// </summary>
    AudioVolumeDown = 163,

    /// <summary>
    /// <c>Mute</c>.
    /// </summary>
    AudioVolumeMute = 164,

    /// <summary>
    /// <c>Volume Up</c>.
    /// </summary>
    AudioVolumeUp = 165,

    /// <summary>
    /// <c>Wake Up</c>.
    /// </summary>
    WakeUp = 166,


    // ###########
    // Legacy Keys
    // ###########

    /// <summary>
    /// <c>Hyper</c>.
    /// Legacy.
    /// </summary>
    Hyper = 167,

    /// <summary>
    /// <c>Super</c>.
    /// Legacy.
    /// </summary>
    Super = 168,

    /// <summary>
    /// <c>Turbo</c>.
    /// Legacy.
    /// </summary>
    Turbo = 169,

    /// <summary>
    /// <c>Abort</c>.
    /// Legacy.
    /// </summary>
    Abort = 170,

    /// <summary>
    /// <c>Resume</c>.
    /// Legacy.
    /// </summary>
    Resume = 171,

    /// <summary>
    /// <c>Suspend</c>.
    /// Legacy.
    /// </summary>
    Suspend = 172,

    /// <summary>
    /// <c>Again</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Again = 173,

    /// <summary>
    /// <c>Copy</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Copy = 174,

    /// <summary>
    /// <c>Cut</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Cut = 175,

    /// <summary>
    /// <c>Find</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Find = 176,

    /// <summary>
    /// <c>Open</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Open = 177,

    /// <summary>
    /// <c>Paste</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Paste = 178,

    /// <summary>
    /// <c>Props</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Props = 179,

    /// <summary>
    /// <c>Select</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Select = 180,

    /// <summary>
    /// <c>Undo</c>.
    /// Legacy.
    /// Found on Sun’s USB keyboard.
    /// </summary>
    Undo = 181,

    /// <summary>
    /// Use for dedicated <c>ひらがな</c> key found on some Japanese word processing keyboards.
    /// Legacy.
    /// </summary>
    Hiragana = 182,

    /// <summary>
    /// Use for dedicated <c>カタカナ</c> key found on some Japanese word processing keyboards.
    /// Legacy.
    /// </summary>
    Katakana = 183

}
