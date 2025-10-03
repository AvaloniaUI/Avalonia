namespace Avalonia.Data;

/// <summary>
/// Base class for the various types of binding supported by Avalonia.
/// </summary>
public abstract class BindingBase
{
    /// <summary>
    /// Creates a <see cref="BindingExpressionBase"/> from a binding.
    /// </summary>
    /// <param name="target">The target of the binding.</param>
    /// <param name="targetProperty">The target property of the binding.</param>
    /// <param name="anchor">
    /// If <paramref name="target"/> is not a control, provides an anchor object from which to
    /// locate a data context or other controls.
    /// </param>
    /// <returns>
    /// A newly instantiated <see cref="BindingExpressionBase"/>.
    /// </returns>
    /// <remarks>
    /// This is a low-level method which returns a binding expression that is not yet connected to
    /// a binding sink, and so is inactive.
    /// </remarks>
    internal abstract BindingExpressionBase CreateInstance(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor);
}
