namespace Avalonia.Input;

/// <summary>
/// Known values of <see cref="PlatformKeyInfo.Format"/>. Each value identifies the platform that produced a
/// <see cref="PlatformKeyInfo"/>, and so the meaning of its values. Other platforms can report other formats.
/// </summary>
public static class KnownPlatformKeyInfoFormats
{
    /// <summary>
    /// Win32 keyboard messages.
    /// </summary>
    public static readonly string Win32 = nameof(Win32);

    /// <summary>
    /// X11 key events.
    /// </summary>
    public static readonly string X11 = nameof(X11);

    /// <summary>
    /// Wayland <c>wl_keyboard</c> events.
    /// </summary>
    public static readonly string Wayland = nameof(Wayland);

    /// <summary>
    /// macOS <c>NSEvent</c> key events.
    /// </summary>
    public static readonly string MacOS = nameof(MacOS);
}

/// <summary>
/// Platform-specific data of a key event, for code that knows the platform, for example terminal emulators that
/// encode keys the way a native terminal does. Each value is null when the platform doesn't provide it.
/// </summary>
public sealed class PlatformKeyInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformKeyInfo"/> class.
    /// </summary>
    /// <param name="format">The platform that produced the values, usually one of
    /// <see cref="KnownPlatformKeyInfoFormats"/>.</param>
    public PlatformKeyInfo(string format)
    {
        Format = format;
    }

    /// <summary>
    /// Gets the platform that produced the values, usually one of <see cref="KnownPlatformKeyInfoFormats"/>.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets the scan code of the key.
    /// </summary>
    /// <remarks>
    /// Win32: the hardware scan code, plus 0xE000 for extended keys.<br/>
    /// X11 and Wayland: the XKB keycode (evdev code + 8).<br/>
    /// macOS: <c>NSEvent.keyCode</c>, which identifies the physical key on every layout. Qt reports it as the native
    /// virtual key.
    /// </remarks>
    public uint? ScanCode { get; init; }

    /// <summary>
    /// Gets the virtual key.
    /// </summary>
    /// <remarks>
    /// Win32: the virtual-key code.<br/>
    /// X11 and Wayland: the keysym.<br/>
    /// macOS: null, because macOS has no layout-dependent key code. See <see cref="ScanCode"/>.
    /// </remarks>
    public uint? VirtualKey { get; init; }

    /// <summary>
    /// Gets the modifier state.
    /// </summary>
    /// <remarks>
    /// Win32: console <c>dwControlKeyState</c> flags (LEFT_CTRL_PRESSED, RIGHT_ALT_PRESSED, ENHANCED_KEY, ...),
    /// including the key being pressed.<br/>
    /// X11: <c>XKeyEvent.state</c>, the state before the event.<br/>
    /// Wayland: the effective XKB modifier mask, the state before the event.<br/>
    /// macOS: <c>NSEventModifierFlags</c>.<br/>
    /// Only the core XKB modifiers (Shift, Lock, Control, Mod1 to Mod5) have fixed bits. What Mod1 to Mod5 mean
    /// depends on the XKB keymap.
    /// </remarks>
    public ulong? Modifiers { get; init; }
}
