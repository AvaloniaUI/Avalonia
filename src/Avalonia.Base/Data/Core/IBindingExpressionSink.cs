namespace Avalonia.Data.Core;

internal interface IBindingExpressionSink
{
    /// <summary>
    /// Called when an <see cref="UntypedBindingExpressionBase"/>'s value or error state
    /// changes.
    /// </summary>
    /// <param name="instance">The binding expression.</param>
    /// <param name="hasValueChanged">
    /// Indicates whether the binding has produced a new value.
    /// </param>
    /// <param name="hasErrorChanged">
    /// Indicates whether the binding has produced a new error.
    /// </param>
    void OnChanged(
        BindingExpressionBase instance,
        bool hasValueChanged,
        bool hasErrorChanged);

    /// <summary>
    /// Called when a <see cref="BindingExpressionBase"/> completes.
    /// </summary>
    /// <param name="instance">The binding expression.</param>
    void OnCompleted(BindingExpressionBase instance);
}
