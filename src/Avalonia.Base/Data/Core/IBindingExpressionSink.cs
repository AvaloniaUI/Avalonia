namespace Avalonia.Data.Core;

internal interface IBindingExpressionSink
{
    /// <summary>
    /// Called when an <see cref="UntypedBindingExpressionBase"/>'s value or error state
    /// changes.
    /// </summary>
    /// <param name="instance">The binding expression.</param>
    /// <param name="hasValueChanged">
    /// Indicates whether <paramref name="value"/> represents a new value produced by the binding.
    /// </param>
    /// <param name="hasErrorChanged">
    /// Indicates whether <paramref name="error"/> represents a new error produced by the binding.
    /// </param>
    /// <param name="value">
    /// The new binding value; if <paramref name="hasValueChanged"/> is true.
    /// </param>
    /// <param name="error">
    /// The new binding error; if <paramref name="hasErrorChanged"/> is true.
    /// </param>
    void OnChanged(
        UntypedBindingExpressionBase instance,
        bool hasValueChanged,
        bool hasErrorChanged,
        object? value,
        BindingError? error);

    /// <summary>
    /// Called when an <see cref="UntypedBindingExpressionBase"/> completes.
    /// </summary>
    /// <param name="instance">The binding expression.</param>
    void OnCompleted(UntypedBindingExpressionBase instance);
}
