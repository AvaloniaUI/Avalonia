using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    /// Performs a deep clone of the expression node.
    /// </summary>
    public abstract ExpressionNode Clone();

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
        if (_source?.TryGetTarget(out var oldSource) != true)
            oldSource = AvaloniaProperty.UnsetValue;

        if (source == oldSource)
            return;

        if (oldSource is not null && oldSource != AvaloniaProperty.UnsetValue)
            Unsubscribe(oldSource);

        if (source == AvaloniaProperty.UnsetValue)
        {
            // If the source is unset then the value is unset. We explicitly do not want to call
            // OnSourceChanged as we don't want to raise errors for subsequent nodes in the
            // binding change.
            _source = null;
            _value = AvaloniaProperty.UnsetValue;
        }
        else
        {
            _source = new(source);
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
                {
                    if (notification.Value is BindingNotification n)
                        SetValue(n);
                    else
                        SetValue(notification.Value, notification.Error);
                }
                else
                {
                    SetDataValidationError(notification.Error!);
                }
            }
            else
            {
                SetValue(notification.Value);
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
        _value = value;
        Owner?.OnNodeValueChanged(Index, value, dataValidationError);
    }

    /// <summary>
    /// Called from <see cref="OnSourceChanged(object?, Exception?)"/> to validate that the source
    /// is non-null and raise a node error if it is not.
    /// </summary>
    /// <param name="source">The expression node source.</param>
    /// <returns>
    /// True if the source is non-null; otherwise, false.
    /// </returns>
    protected bool ValidateNonNullSource([NotNullWhen(true)] object? source)
    {
        if (source is null)
        {
            Owner?.OnNodeError(Index - 1, "Value is null.");
            _value = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// When implemented in a derived class, subscribes to the new source, and updates the current 
    /// <see cref="Value"/>.
    /// </summary>
    /// <param name="source">The new source.</param>
    /// <param name="dataValidationError">
    /// Any data validation error reported by the previous expression node.
    /// </param>
    protected abstract void OnSourceChanged(object? source, Exception? dataValidationError);

    /// <summary>
    /// When implemented in a derived class, unsubscribes from the previous source.
    /// </summary>
    /// <param name="oldSource">The old source.</param>
    protected virtual void Unsubscribe(object oldSource) { }
}
