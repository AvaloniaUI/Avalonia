using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="BindingExpression"/>.
/// </summary>
internal abstract class ExpressionNode
{
    private WeakReference<object?>? _source;
    private object? _value = AvaloniaProperty.UnsetValue;

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
    public object? Value => _value;

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
        SetSource(null, null);
        _source = null;
        _value = AvaloniaProperty.UnsetValue;
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
    /// <param name="dataValidationError">
    /// Any data validation error reported by the previous expression node.
    /// </param>
    public void SetSource(object? source, Exception? dataValidationError)
    {
        var oldSource = Source;

        if (source == AvaloniaProperty.UnsetValue)
            source = null;

        if (source == oldSource)
            return;

        if (oldSource is not null)
            Unsubscribe(oldSource);

        _source = new(source);

        if (source is null)
        {
            // If the source is null then the value is null. We explicitly do not want to call
            // OnSourceChanged as we don't want to raise errors for subsequent nodes in the
            // binding change.
            _value = AvaloniaProperty.UnsetValue;
        }
        else
        {
            try { OnSourceChanged(source, dataValidationError); }
            catch (Exception e) { SetError(e); }
        }
    }

    /// <summary>
    /// Sets the current value to <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </summary>
    protected void ClearValue() => SetValue(AvaloniaProperty.UnsetValue);

    /// <summary>
    /// Notifies the <see cref="Owner"/> of a data validation error.
    /// </summary>
    /// <param name="error">The error.</param>
    protected void SetDataValidationError(Exception error)
    {
        if (error is TargetInvocationException tie)
            error = tie.InnerException!;
        Owner?.OnDataValidationError(error);
    }

    /// <summary>
    /// Sets the current value to <see cref="AvaloniaProperty.UnsetValue"/> and notifies the
    /// <see cref="Owner"/> of the error.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected void SetError(string message)
    {
        _value = AvaloniaProperty.UnsetValue;
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
        if (e is AggregateException ae && ae.InnerExceptions.Count == 1)
            e = e.InnerException!;
        SetError(e.Message);
    }

    /// <summary>
    /// Sets the current <see cref="Value"/>, notifying the <see cref="Owner"/> if the value
    /// has changed.
    /// </summary>
    /// <param name="valueOrNotification">
    /// The new value. May be a <see cref="BindingNotification"/>.
    /// </param>
    protected void SetValue(object? valueOrNotification)
    {
        if (valueOrNotification is BindingNotification notification)
        {
            if (notification.ErrorType == BindingErrorType.Error)
            {
                SetError(notification.Error!);
            }
            else if (notification.ErrorType == BindingErrorType.DataValidationError)
            {
                if (notification.HasValue)
                    SetValue(notification.Value, notification.Error);
                else
                    SetDataValidationError(notification.Error!);
            }
            else
            {
                SetValue(notification.Value, null);
            }
        }
        else
        {
            SetValue(valueOrNotification, null);
        }
    }

    /// <summary>
    /// Sets the current <see cref="Value"/>, notifying the <see cref="Owner"/> if the value
    /// has changed.
    /// </summary>
    /// <param name="value">
    /// The new value. May not be a <see cref="BindingNotification"/>.
    /// </param>
    /// <param name="dataValidationError">
    /// The data validation error associated with the new value, if any.
    /// </param>
    protected void SetValue(object? value, Exception? dataValidationError = null)
    {
        Debug.Assert(value is not BindingNotification);

        if (Owner is null)
            return;

        // We raise a change notification if:
        //
        // - This is the initial value (_value is null)
        // - There is a data validation error
        // - There is no data validation error, but the owner has one
        // - The new value is different to the old value
        if (_value is null ||
            dataValidationError is not null ||
            (dataValidationError is null && Owner.ErrorType == BindingErrorType.DataValidationError) ||
            !Equals(value, _value))
        {
            _value = value;
            Owner.OnNodeValueChanged(Index, value, dataValidationError);
        }
    }

    /// <summary>
    /// When implemented in a derived class, subscribes to the new source, and updates the current 
    /// <see cref="Value"/>.
    /// </summary>
    /// <param name="source">The new source.</param>
    /// <param name="dataValidationError">
    /// Any data validation error reported by the previous expression node.
    /// </param>
    protected abstract void OnSourceChanged(object source, Exception? dataValidationError);

    /// <summary>
    /// When implemented in a derived class, unsubscribes from the previous source.
    /// </summary>
    /// <param name="oldSource">The old source.</param>
    protected virtual void Unsubscribe(object oldSource) { }
}
