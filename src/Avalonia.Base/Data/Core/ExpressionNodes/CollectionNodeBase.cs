using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes;

internal abstract class CollectionNodeBase : ExpressionNode,
    IWeakEventSubscriber<NotifyCollectionChangedEventArgs>,
    IWeakEventSubscriber<PropertyChangedEventArgs>
{
    void IWeakEventSubscriber<NotifyCollectionChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
    {
        if (ShouldUpdate(sender, e))
            UpdateValueOrSetError(sender);
    }

    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        if (ShouldUpdate(sender, e))
            UpdateValueOrSetError(sender);
    }

    protected override void OnSourceChanged(object source, Exception? dataValidationError)
    {
        Subscribe(source);
        UpdateValue(source);
    }

    protected override void Unsubscribe(object source)
    {
        if (source is INotifyCollectionChanged incc)
            WeakEvents.CollectionChanged.Unsubscribe(incc, this);
        if (source is INotifyPropertyChanged inpc)
            WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
    }

    protected abstract bool ShouldUpdate(object? sender, PropertyChangedEventArgs e);
    protected abstract int? TryGetFirstArgumentAsInt();
    protected abstract void UpdateValue(object? source);

    private bool ShouldUpdate(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender != Source)
            return false;

        if (sender is IList && TryGetFirstArgumentAsInt() is int index)
        {
            return e.Action switch
            {
                NotifyCollectionChangedAction.Add => index >= e.NewStartingIndex,
                NotifyCollectionChangedAction.Remove => index >= e.OldStartingIndex,
                NotifyCollectionChangedAction.Replace =>
                    index >= e.NewStartingIndex &&
                    index < e.NewStartingIndex + e.NewItems!.Count,
                NotifyCollectionChangedAction.Move =>
                    index >= e.NewStartingIndex && index < e.NewStartingIndex + e.NewItems!.Count ||
                    index >= e.OldStartingIndex && index < e.OldStartingIndex + e.OldItems!.Count,
                _ => true,
            };
        }

        // Implementation defined meaning for the index, so just try to update anyway
        return true;
    }

    private void Subscribe(object? source)
    {
        if (source is INotifyCollectionChanged incc)
            WeakEvents.CollectionChanged.Subscribe(incc, this);
        if (source is INotifyPropertyChanged inpc)
            WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
    }

    private void UpdateValueOrSetError(object? source)
    {
        try { UpdateValue(source); }
        catch (Exception e) { SetError(e); }
    }
}
