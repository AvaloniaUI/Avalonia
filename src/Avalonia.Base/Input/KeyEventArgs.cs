using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Provides information specific to a keyboard event.
/// </summary>
public class KeyEventArgs : RoutedEventArgs, IKeyModifiersEventArgs
{
    /// <summary>
    /// <para>
    /// Gets the virtual-key for the associated event.<br/>
    /// A given physical key can result in different virtual keys depending on the current keyboard layout.<br/>
    /// This is the key that is generally referred to when creating keyboard shortcuts.
    /// </para>
    /// <para>
    /// For example, when pressing the key located at the <c>Z</c> position on standard US English QWERTY keyboard,
    /// this property returns:<br/>
    /// - <see cref="Input.Key.Z"/> for an English (QWERTY) layout<br/>
    /// - <see cref="Input.Key.W"/> for a French (AZERTY) layout<br/>
    /// - <see cref="Input.Key.Y"/> for a German (QWERTZ) layout<br/>
    /// - <see cref="Input.Key.Z"/> for a Russian (JCUKEN) layout
    /// </para>
    /// </summary>
    /// <remarks>
    /// This property should be used for letter-related shortcuts only.<br/>
    /// Prefer using <see cref="PhysicalKey"/> if you need to refer to a key given its position on the keyboard (a
    /// common usage is moving the player with WASD-like keys in games), or <see cref="KeySymbol"/> if you want to know
    /// which character the key will output.<br/>
    /// Avoid using this for shortcuts related to punctuation keys, as they differ wildly depending on keyboard layouts.
    /// </remarks>
    /// <seealso cref="PhysicalKey"/>
    /// <seealso cref="KeySymbol"/>
    public Key Key { get; init; }

    public KeyModifiers KeyModifiers { get; init; }

    /// <summary>
    /// <para>
    /// Gets the physical key for the associated event.
    /// </para>
    /// <para>
    /// This value is independent of the current keyboard layout and usually correspond to the key printed on a standard
    /// US English QWERTY keyboard.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Use this property if you need to refer to a key given its position on the keyboard (a common usage is moving the
    /// player with WASD-like keys).
    /// </remarks>
    /// <seealso cref="Key"/>
    /// <seealso cref="KeySymbol"/>
    public PhysicalKey PhysicalKey { get; init; }

    /// <summary>
    /// <para>
    /// Gets the unicode symbol of the key, or null if none is applicable.
    /// </para>
    /// <para>
    /// For example, when pressing the key located at the <c>Z</c> position on standard US English QWERTY keyboard,
    /// this property returns:<br/>
    /// - <c>z</c> for an English (QWERTY) layout<br/>
    /// - <c>w</c> for a French (AZERTY) layout<br/>
    /// - <c>y</c> for a German (QWERTZ) layout<br/>
    /// - <c>я</c> for a Russian (JCUKEN) layout
    /// </para>
    /// </summary>
    /// <see cref="Key"/>
    /// <see cref="PhysicalKey"/>
    public string? KeySymbol { get; init; }

    /// <summary>
    /// Type of the device that fire the event
    /// </summary>
    public KeyDeviceType KeyDeviceType { get; init; }

    /// <summary>
    /// <para>
    /// Gets the text that the platform attached to this key event, with all active modifiers applied, when it is
    /// available. It includes C0 control characters and DEL.
    /// </para>
    /// <para>
    /// For example, Ctrl+Z produces <c>"\x1a"</c> on most layouts. On a German layout, AltGr+Q produces <c>"@"</c> on
    /// Windows and Linux, and Option+L produces <c>"@"</c> on macOS.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is intended for encoding keys, for example in terminal emulators. It is not a replacement for
    /// <see cref="InputElement.TextInputEvent"/>:
    /// </para>
    /// <list type="bullet">
    /// <item>Null doesn't mean that no text input follows. Some platforms don't provide the value, and keys that an
    /// input method handles have none.</item>
    /// <item>It is not a preview of, or a guarantee for, the committed text.</item>
    /// <item>It can contain several characters, surrogate pairs and NUL characters.</item>
    /// <item>The result of a dead key or compose sequence depends on the platform: some platforms report the
    /// composed text, others report the text before the input method handles the event.</item>
    /// </list>
    /// <para>
    /// The value comes from the <c>WM_CHAR</c> messages of the key on Windows, <c>XLookupString</c> on X11,
    /// <c>xkb_state_key_get_utf8</c> on Wayland and <c>NSEvent.characters</c> on macOS. It matches the text of Qt's
    /// <c>QKeyEvent</c>. On macOS, Option types characters, so Option+Q on a German layout gives <c>"«"</c>.
    /// </para>
    /// <para>It is only set for <see cref="InputElement.KeyDownEvent"/>.</para>
    /// </remarks>
    /// <seealso cref="KeyTextIgnoringModifiers"/>
    public string? KeyText { get; init; }

    /// <summary>
    /// <para>
    /// Gets the text of the key with Ctrl, Alt and Meta ignored and the other modifiers applied, or null if the key
    /// has no text. Like <c>NSEvent.charactersIgnoringModifiers</c> on macOS, the value keeps Shift.
    /// </para>
    /// <para>
    /// For example, Ctrl+Z produces <c>"z"</c>, Ctrl+Shift+2 on a US layout produces <c>"@"</c>, Ctrl+Я on a
    /// Russian layout produces <c>"я"</c>, AltGr+Q on a German layout produces <c>"@"</c> on Windows and Linux, and
    /// Option+B on macOS produces <c>"b"</c>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>The rule in detail:</para>
    /// <list type="bullet">
    /// <item>Shift, Caps Lock and AltGr (ISO Level 3) are applied.</item>
    /// <item>Ctrl, Alt and Meta are ignored, except where they select the key's level. On Windows, AltGr is reported
    /// as Ctrl+Alt, and a Ctrl+Alt chord that produces printable text gives that text. A layout can also use Ctrl or
    /// Alt to select a level, and then the text is the text of that level.</item>
    /// <item>On macOS, Option counts as Alt and is always ignored, so a terminal can use it as a Meta key. This is
    /// the same as <c>NSEvent.charactersIgnoringModifiers</c>: Option+Q on a German layout gives <c>"q"</c>. Use
    /// <see cref="KeyText"/> for the character that Option types (<c>"«"</c>).</item>
    /// </list>
    /// <para>
    /// A terminal emulator can use it to send <c>ESC</c> and the text for Alt chords, or a C0 control character for
    /// Ctrl chords that <see cref="KeyText"/> doesn't cover. <see cref="ConsumedModifiers"/> tells which of the
    /// pressed modifiers the layout used.
    /// </para>
    /// <para>
    /// It is only set for <see cref="InputElement.KeyDownEvent"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="KeyText"/>
    public string? KeyTextIgnoringModifiers { get; init; }

    /// <summary>
    /// <para>
    /// Gets the pressed modifiers that the keyboard layout used to produce the key's text, or null if the platform
    /// doesn't report them.
    /// </para>
    /// <para>
    /// For example, Shift is consumed for Shift+2 (<c>"@"</c>) on a US layout, and Ctrl and Alt are consumed for
    /// AltGr+Q (<c>"@"</c>) on a German layout on Windows. Ctrl is not consumed for Ctrl+Z.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>KeyModifiers &amp; ~ConsumedModifiers</c> gives the modifiers that are left for the application, for example
    /// to decide whether a key types text or is a shortcut.
    /// </para>
    /// <para>
    /// On X11 and Wayland, the value follows the rules of GTK (xkbcommon's <c>XKB_CONSUMED_MODE_GTK</c>): a modifier
    /// is consumed only when it changes the key's keysym. On macOS, Option is consumed when it changes the character,
    /// and Ctrl and Command are never consumed. On Windows, a modifier is consumed when releasing it changes the
    /// key's character in the layout. It is only set for <see cref="InputElement.KeyDownEvent"/>.
    /// </para>
    /// </remarks>
    public KeyModifiers? ConsumedModifiers { get; init; }

    /// <summary>
    /// Gets platform-specific data of the key event, or null if the platform doesn't provide any.
    /// </summary>
    public PlatformKeyInfo? PlatformKeyInfo { get; init; }
}
