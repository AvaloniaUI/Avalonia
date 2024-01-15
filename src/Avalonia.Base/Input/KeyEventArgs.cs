using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Provides information specific to a keyboard event.
/// </summary>
public class KeyEventArgs : RoutedEventArgs
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

    /// <summary>
    /// Gets the key modifiers for the associated event.
    /// </summary>
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
}
