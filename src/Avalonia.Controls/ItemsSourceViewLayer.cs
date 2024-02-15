﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Controls;

public abstract class ItemsSourceViewLayer : AvaloniaObject, INamed
{
    private InvalidationPropertiesCollection? _invalidationPropertyNames;
    private object? _state;

    public string? Name { get; init; }

    /// <summary>
    /// Gets the owner of this layer. This is the owner of the <see cref="ItemsSourceView"/> that to which this layer
    /// has been added.
    /// </summary>
    public AvaloniaObject? Owner => InheritanceParent;

    /// <summary>
    /// Gets or sets a collection of strings which will trigger re-evaluation of an item, if:
    /// <list type="number">
    /// <item>The item implements <see cref="INotifyPropertyChanged"/>; and</item>
    /// <item>The item raises <see cref="INotifyPropertyChanged.PropertyChanged"/>; and</item>
    /// <item>The value of the <see cref="PropertyChangedEventArgs.PropertyName"/> property is found in this collection.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Performance warning: if any strings are added to this collection, the <see cref="INotifyPropertyChanged.PropertyChanged"/> event will be subscribed to on ALL
    /// <see cref="INotifyPropertyChanged"/> items in <see cref="ItemsSourceView.Source"/>. This can lead to a large number of allocations.
    /// </remarks>
    public InvalidationPropertiesCollection InvalidationPropertyNames
    {
        get => _invalidationPropertyNames ??= new();
        init
        {
            _invalidationPropertyNames = value;
            OnInvalidated();
        }
    }

    /// <summary>
    /// Raised when this layer should be re-evaluated for all items in the view.
    /// </summary>
    public event EventHandler<EventArgs>? Invalidated;

    internal protected IEnumerable<string> GetInvalidationPropertyNamesEnumerator() => _invalidationPropertyNames ?? Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets an abitrary object. When the value of this property changes, <see cref="Invalidated"/> is raised.
    /// </summary>
    public object? State
    {
        get => _state;
        set => SetAndRaise(StateProperty, ref _state, value);
    }

    /// <seealso cref="State"/>
    public static readonly DirectProperty<ItemsSourceViewLayer, object?> StateProperty =
        AvaloniaProperty.RegisterDirect<ItemsSourceViewLayer, object?>(nameof(State), o => o.State, (o, v) => o.State = v);

    /// <summary>
    /// Raises the <see cref="Invalidated"/> event.
    /// </summary>
    protected virtual void OnInvalidated()
    {
        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StateProperty)
        {
            OnInvalidated();
        }
    }

    internal void Attach(AvaloniaObject anchor)
    {
        if (InheritanceParent != null && InheritanceParent != anchor)
        {
            throw new InvalidOperationException("This view layer is already attached to another object.");
        }

        InheritanceParent = anchor;
    }

    internal void Detach(AvaloniaObject anchor)
    {
        if (InheritanceParent != anchor)
        {
            throw new ArgumentException("Not attached to this object", nameof(anchor));
        }
        InheritanceParent = null;
    }
}

[AvaloniaList(Separators = new[] { " ", "," }, SplitOptions = StringSplitOptions.RemoveEmptyEntries)]
public class InvalidationPropertiesCollection : AvaloniaList<string>
{
    // Don't validate items: the PropertyChanged event can be raised with any "PropertyName" string.
}