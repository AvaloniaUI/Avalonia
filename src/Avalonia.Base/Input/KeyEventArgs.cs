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
    /// - <see cref="Input.PhysicalKey.KeyZ"/> for an English (QWERTY) layout<br/>
    /// - <see cref="Input.PhysicalKey.KeyW"/> for a French (AZERTY) layout<br/>
    /// - <see cref="Input.PhysicalKey.KeyY"/> for a German (QWERTZ) layout<br/>
    /// - <see cref="Input.PhysicalKey.KeyZ"/> for a Russian (JCUKEN) layout
    /// </para>
    /// </summary>
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
    public PhysicalKey PhysicalKey { get; init; }

    /// <summary>
    /// <para>
    /// Gets the unicode symbol of the key, or null if none is applicable.
    /// </para>
    /// <para>
    /// For example, when pressing the key located at the <c>Z</c> position on standard US English QWERTY keyboard,
    /// this property returns:<br/>
    /// - <c>Z</c> for an English (QWERTY) layout<br/>
    /// - <c>W</c> for a French (AZERTY) layout<br/>
    /// - <c>Y</c> for a German (QWERTZ) layout<br/>
    /// - <c>Я</c> for a Russian (JCUKEN) layout
    /// </para>
    /// </summary>
    public string? KeySymbol { get; init; }
}
