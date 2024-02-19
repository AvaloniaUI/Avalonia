namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in a binding expression which represents the source of the binding, e.g. DataContext,
/// logical ancestor.
/// </summary>
internal abstract class SourceNode : ExpressionNode
{
    /// <summary>
    /// Selects the source for the binding expression based on the binding source, target and
    /// anchor.
    /// </summary>
    /// <param name="source">The binding source.</param>
    /// <param name="target">The binding target.</param>
    /// <param name="anchor">The anchor.</param>
    /// <returns>The source for the binding expression.</returns>
    public virtual object? SelectSource(object? source, object target, object? anchor)
    {
        return source != AvaloniaProperty.UnsetValue ? source : target;
    }

    public virtual bool ShouldLogErrors(object target)
    {
        return Value is not null;
    }
}
