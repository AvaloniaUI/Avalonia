namespace Avalonia.Data;

/// <summary>
/// Base class for the various types of binding supported by Avalonia.
/// </summary>
public abstract class BindingBase
{
    internal abstract BindingExpressionBase Instance(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor);
}
