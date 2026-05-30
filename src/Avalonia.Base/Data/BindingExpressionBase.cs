using System;
using Avalonia.Data.Core;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Data;

/// <summary>
/// Represents the base class for binding expressions.
/// </summary>
/// <remarks>
/// A binding expression represents an instantiation of a binding on an object.
/// </remarks>
public abstract class BindingExpressionBase : IDisposable, ISetterInstance, IValueEntry
{
    private protected BindingExpressionBase(BindingPriority defaultPriority)
    {
        DefaultPriority = defaultPriority;
    }

    /// <summary>
    /// Gets the priority of the binding expression.
    /// </summary>
    public BindingPriority Priority { get; private protected set; }

    /// <summary>
    /// Gets the <see cref="AvaloniaProperty"/> that the binding expression is targeting.
    /// </summary>
    public AvaloniaProperty? TargetProperty { get; private protected set; }

    /// <summary>
    /// Gets the default priority of the binding expression.
    /// </summary>
    /// <remarks>
    /// This property describes the preferred priority of the binding expression; the priority
    /// passed to the <see cref="Attach"/> method may differ if the binding targets a direct
    /// property, in which case the priority will be elevated to
    /// <see cref="BindingPriority.LocalValue"/>.
    /// </remarks>
    internal BindingPriority DefaultPriority { get; }

    /// <summary>
    /// Gets a value indicating whether data validation is enabled for the binding expression.
    /// </summary>
    internal bool IsDataValidationEnabled { get; private protected set; }

    AvaloniaProperty IValueEntry.Property => TargetProperty ??
        throw new InvalidOperationException("The binding expression is not attached.");

    bool IValueEntry.HasValue() => HasValue();
    object? IValueEntry.GetValue() => GetUntypedValue();
    bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error) =>
        GetDataValidationState(out state, out error);
    void IValueEntry.Unsubscribe() => Unsubscribe();

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sends the current binding target value to the binding source property in 
    /// <see cref="BindingMode.TwoWay"/> or <see cref="BindingMode.OneWayToSource"/> bindings.
    /// </summary>
    /// <remarks>
    /// This method does nothing when the Mode of the binding is not 
    /// <see cref="BindingMode.TwoWay"/> or <see cref="BindingMode.OneWayToSource"/>.
    /// 
    /// If the UpdateSourceTrigger value of your binding is set to
    /// <see cref="UpdateSourceTrigger.Explicit"/>, you must call the 
    /// <see cref="UpdateSource"/> method or the changes will not propagate back to the
    /// source.
    /// </remarks>
    public virtual void UpdateSource() { }

    /// <summary>
    /// Forces a data transfer from the binding source to the binding target.
    /// </summary>
    public virtual void UpdateTarget() { }

    /// <summary>
    /// When overridden in a derived class, attaches the binding expression to a value store but
    /// does not start it.
    /// </summary>
    /// <param name="sink">The binding expression sink to attach to.</param>
    /// <param name="frame">The immediate value frame to attach to, if any.</param>
    /// <param name="target">The target object.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="priority">The priority of the binding.</param>
    internal abstract void Attach(
        IBindingExpressionSink sink,
        ImmediateValueFrame? frame,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority);

    /// <summary>
    /// Starts the binding expression.
    /// </summary>
    /// <param name="produceValue">
    /// Indicates whether the binding expression should produce an initial value.
    /// </param>
    internal abstract void Start(bool produceValue);

    /// <summary>
    /// Checks whether the binding expression has a value, starting it if necessary.
    /// </summary>
    private protected abstract bool HasValue();

    /// <summary>
    /// Gets the current value of the binding expression as a boxed object.
    /// </summary>
    private protected abstract object? GetUntypedValue();

    /// <summary>
    /// Gets the data validation state of the binding expression, if supported.
    /// </summary>
    private protected abstract bool GetDataValidationState(out BindingValueType state, out Exception? error);

    /// <summary>
    /// Called when the binding expression is removed from the value store as a value entry.
    /// </summary>
    private protected abstract void Unsubscribe();
}
