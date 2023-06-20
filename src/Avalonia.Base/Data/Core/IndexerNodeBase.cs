using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Data.Core
{
    internal abstract class IndexerNodeBase : SettableNode,
        IWeakEventSubscriber<NotifyCollectionChangedEventArgs>,
        IWeakEventSubscriber<PropertyChangedEventArgs>
    {
        protected override void StartListeningCore(WeakReference<object?> reference)
        {
            reference.TryGetTarget(out var target);

            if (target is INotifyCollectionChanged incc)
            {
                WeakEvents.CollectionChanged.Subscribe(incc, this);
            }

            if (target is INotifyPropertyChanged inpc)
            {
                WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
            }
            
            ValueChanged(GetValue(target));
        }

        protected override void StopListeningCore()
        {
            if (Target.TryGetTarget(out var target))
            {
                if (target is INotifyCollectionChanged incc)
                {
                    WeakEvents.CollectionChanged.Unsubscribe(incc, this);
                }

                if (target is INotifyPropertyChanged inpc)
                {
                    WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
                }
            }
        }

        protected abstract object? GetValue(object? target);

        protected abstract int? TryGetFirstArgumentAsInt();

        private bool ShouldUpdate(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is IList)
            {
                var index = TryGetFirstArgumentAsInt();

                if (index == null)
                {
                    return false;
                }

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        return index >= e.NewStartingIndex;
                    case NotifyCollectionChangedAction.Remove:
                        return index >= e.OldStartingIndex;
                    case NotifyCollectionChangedAction.Replace:
                        return index >= e.NewStartingIndex &&
                               index < e.NewStartingIndex + e.NewItems!.Count;
                    case NotifyCollectionChangedAction.Move:
                        return (index >= e.NewStartingIndex &&
                                index < e.NewStartingIndex + e.NewItems!.Count) ||
                               (index >= e.OldStartingIndex &&
                                index < e.OldStartingIndex + e.OldItems!.Count);
                    case NotifyCollectionChangedAction.Reset:
                        return true;
                }
            }

            return true; // Implementation defined meaning for the index, so just try to update anyway
        }

        protected abstract bool ShouldUpdate(object? sender, PropertyChangedEventArgs e);

        void IWeakEventSubscriber<NotifyCollectionChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
        {
            if (ShouldUpdate(sender, e))
            {
                ValueChanged(GetValue(sender));
            }
        }

        void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
        {
            if (ShouldUpdate(sender, e))
            {
                ValueChanged(GetValue(sender));
            }
        }
    }
}
