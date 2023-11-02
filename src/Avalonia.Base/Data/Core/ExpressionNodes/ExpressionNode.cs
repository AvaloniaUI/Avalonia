using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="BindingExpression"/>.
/// </summary>
internal abstract class ExpressionNode
{
    private WeakReference<object?>? _source;
    private WeakReference<object?>? _value;

    /// <summary>
    /// Gets the index of the node in the binding path.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Gets the owning <see cref="BindingExpression"/>.
    /// </summary>
    public BindingExpression? Owner { get; private set; }

    /// <summary>
    /// Gets the source object from which the node will read its value.
    /// </summary>
    public object? Source
    {
        get
        {
            if (_source?.TryGetTarget(out var source) == true)
                return source;
            return null;
        }
    }

    /// <summary>
    /// Gets the current value of the node.
    /// </summary>
    public object? Value
    {
        get
        {
            if (_value is null)
                return AvaloniaProperty.UnsetValue;
            _value.TryGetTarget(out var value);
            return value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the node's <see cref="Value"/> is alive, i.e. 
    /// initialized and not a GC'd object.
    /// </summary>
    public bool IsValueAlive
    {
        get
        {
            return _value == BindingExpression.NullReference || 
                _value?.TryGetTarget(out _) == true;
        }
    }

    /// <summary>
    /// Appends a string representation of the expression node to a string builder.
    /// </summary>
    /// <param name="builder">The string builder.</param>
    public virtual void BuildString(StringBuilder builder) { }

    /// <summary>
    /// Builds a string representation of a binding expression.
    /// </summary>
    /// <param name="builder">The string builder.</param>
    /// <param name="nodes">The nodes in the binding expression.</param>
    public virtual void BuildString(StringBuilder builder, IReadOnlyList<ExpressionNode> nodes) 
    {
        if (Index > 0)
            nodes[Index - 1].BuildString(builder, nodes);
        BuildString(builder);
    }

    /// <summary>
    /// Resets the node to its uninitialized state when the <see cref="Owner"/> is unsubscribed.
    /// </summary>
    public void Reset()
    {
        SetSource(null);
        _source = _value = null;
    }

    /// <summary>
    /// Sets the owner binding.
    /// </summary>
    /// <param name="owner">The owner binding.</param>
    /// <param name="index">The index of the node in the binding path.</param>
    /// <exception cref="InvalidOperationException">
    /// The node already has an owner.
    /// </exception>
    public void SetOwner(BindingExpression owner, int index)
    {
        if (Owner is not null)
            throw new InvalidOperationException($"{this} already has an owner.");
        Owner = owner;
        Index = index;
    }

    /// <summary>
    /// Sets the <see cref="Source"/> from which the node will read its value and updates
    /// the current <see cref="Value"/>, notifying the <see cref="Owner"/> if the value
    /// changes.
    /// </summary>
    /// <param name="source">
    /// The new source from which the node will read its value. May be 
    /// <see cref="AvaloniaProperty.UnsetValue"/> in which case the source will be considered
    /// to be null.
    /// </param>
    public void SetSource(object? source)
    {
        var oldSource = Source;

        if (source == AvaloniaProperty.UnsetValue)
            source = null;

        if (oldSource is not null)
            Unsubscribe(oldSource);

        _source = new(source);

        if (source is null)
        {
            // If the source is null then the value is null. We explicitly do not want to call
            // OnSourceChanged as we don't want to raise errors for subsequent nodes in the
            // binding change.
            _value = BindingExpression.NullReference;
        }
        else if (source != oldSource)
        {
            try { OnSourceChanged(source); }
            catch (Exception e) { SetError(e); }
        }
    }

    /// <summary>
    /// Sets the current value to <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </summary>
    protected void ClearValue() => SetValue(AvaloniaProperty.UnsetValue);

    /// <summary>
    /// Sets the current value to <see cref="AvaloniaProperty.UnsetValue"/> and notifies the
    /// <see cref="Owner"/> of the error.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected void SetError(string message)
    {
        _value = new(AvaloniaProperty.UnsetValue);
        Owner?.OnNodeError(Index, message);
    }

    /// <summary>
    /// Sets the current value to <see cref="AvaloniaProperty.UnsetValue"/> and notifies the
    /// <see cref="Owner"/> of the error.
    /// </summary>
    /// <param name="e">The error.</param>
    protected void SetError(Exception e)
    {
        if (e is TargetInvocationException tie)
            e = tie.InnerException!;
        SetError(e.Message);
    }

    /// <summary>
    /// Sets the current <see cref="Value"/>, notifying the <see cref="Owner"/> if the value
    /// has changed.
    /// </summary>
    /// <param name="value">The new value.</param>
    protected void SetValue(object? value)
    {
        // We raise a change notification if:
        //
        // - This is the initial value (_value is null)
        // - The value is a binding notification
        // - The old value has been GC'd - in this case we don't know if the new value is different
        // - The new value is different to the old value
        if (_value is null ||
            value is BindingNotification ||
            _value.TryGetTarget(out var oldValue) == false ||
            !Equals(oldValue, value))
        {
            _value = value is null ? BindingExpression.NullReference : new(value);
            Owner?.OnNodeValueChanged(Index, value);
        }
    }

    /// <summary>
    /// When implemented in a derived class, subscribes to the new source, and updates the current 
    /// <see cref="Value"/>.
    /// </summary>
    /// <param name="source">The new source.</param>
    protected abstract void OnSourceChanged(object source);

    /// <summary>
    /// When implemented in a derived class, unsubscribes from the previous source.
    /// </summary>
    /// <param name="oldSource">The old source.</param>
    protected virtual void Unsubscribe(object oldSource) { }
}
