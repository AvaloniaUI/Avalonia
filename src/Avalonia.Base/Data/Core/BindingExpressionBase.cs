using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Data.Core;

public abstract class BindingExpressionBase : IBindingExpression, ISetterInstance
{
    private protected BindingExpressionBase()
    {
    }

    internal BindingMode Mode { get; private protected set; }

    public virtual void Dispose()
    {
    }

    /// <summary>
    /// When overridden in a derived class, attaches the binding expression to a value store but
    /// does not start it.
    /// </summary>
    /// <param name="valueStore">The value store to attach to.</param>
    /// <param name="target">The target object.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="priority">The priority of the binding.</param>
    internal abstract void Attach(
        ValueStore valueStore,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority);
}
