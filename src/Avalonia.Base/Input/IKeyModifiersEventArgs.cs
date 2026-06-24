using Avalonia.Metadata;

namespace Avalonia.Input;

/// <summary>
/// Represents an event associated with a set of <see cref="Input.KeyModifiers"/>.
/// </summary>
[NotClientImplementable]
public interface IKeyModifiersEventArgs
{
    /// <summary>
    /// Gets the key modifiers associated with this event.
    /// </summary>
    KeyModifiers KeyModifiers { get; }
}
