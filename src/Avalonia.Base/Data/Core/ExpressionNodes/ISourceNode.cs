namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// An <see cref="ExpressionNode"/> which selects the source of a binding expression.
/// </summary>
internal interface ISourceNode
{
    /// <summary>
    /// Selects the source for the binding expression based on the binding source, target and
    /// anchor.
    /// </summary>
    /// <param name="source">The binding source.</param>
    /// <param name="target">The binding target.</param>
    /// <param name="anchor">The anchor.</param>
    /// <returns>The source for the binding expression.</returns>
    object SelectSource(object? source, object target, object? anchor);
}
