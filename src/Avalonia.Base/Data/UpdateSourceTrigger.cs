namespace Avalonia.Data;

/// <summary>
/// Describes the timing of binding source updates.
/// </summary>
public enum UpdateSourceTrigger
{
    /// <summary>
    /// The default <see cref="UpdateSourceTrigger"/> value of the binding target property.
    /// This currently defaults to <see cref="PropertyChanged"/>.
    /// </summary>
    Default,

    /// <summary>
    /// Updates the binding source immediately whenever the binding target property changes.
    /// </summary>
    PropertyChanged,

    /// <summary>
    /// Updates the binding source whenever the binding target element loses focus.
    /// </summary>
    LostFocus,

    /// <summary>
    /// Updates the binding source only when you call the 
    /// <see cref="BindingExpressionBase.UpdateSource()"/> method.
    /// </summary>
    Explicit,
}
