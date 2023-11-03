namespace Avalonia.Data.Core;

internal interface IBindingExpressionSink
{
    /// <summary>
    /// Called when a <see cref="BindingExpression"/>'s value or error state
    /// changes.
    /// </summary>
    /// <param name="instance">The binding expression.</param>
    void OnChanged(
        BindingExpression instance,
        bool hasValueChanged,
        bool hasErrorChanged);

    /// <summary>
    /// Called when a <see cref="BindingExpression"/> completes.
    /// </summary>
    /// <param name="instance">The binding expression.</param>
    void OnCompleted(BindingExpression instance);
}
