using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Utilities;

namespace Avalonia.Data.Core
{
    public abstract class IndexerNodeBase : SettableNode
    {
        private IDisposable _subscription;
        
        protected override void StartListeningCore(WeakReference<object> reference)
        {
            reference.TryGetTarget(out object target);

            var incc = target as INotifyCollectionChanged;
            var inpc = target as INotifyPropertyChanged;
            var inputs = new List<IObservable<object>>();

            if (incc != null)
            {
                inputs.Add(WeakObservable.FromEventPattern<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>(
                    incc,
                    nameof(incc.CollectionChanged))
                    .Where(x => ShouldUpdate(x.Sender, x.EventArgs))
                    .Select(_ => GetValue(target)));
            }

            if (inpc != null)
            {
                inputs.Add(WeakObservable.FromEventPattern<INotifyPropertyChanged, PropertyChangedEventArgs>(
                    inpc,
                    nameof(inpc.PropertyChanged))
                    .Where(x => ShouldUpdate(x.Sender, x.EventArgs))
                    .Select(_ => GetValue(target)));
            }

            _subscription = Observable.Merge(inputs).StartWith(GetValue(target)).Subscribe(ValueChanged);
        }

        protected override void StopListeningCore()
        {
            _subscription.Dispose();
        }

        protected abstract object GetValue(object target);

        protected abstract int? TryGetFirstArgumentAsInt();

        private bool ShouldUpdate(object sender, NotifyCollectionChangedEventArgs e)
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
                               index < e.NewStartingIndex + e.NewItems.Count;
                    case NotifyCollectionChangedAction.Move:
                        return (index >= e.NewStartingIndex &&
                                index < e.NewStartingIndex + e.NewItems.Count) ||
                               (index >= e.OldStartingIndex &&
                                index < e.OldStartingIndex + e.OldItems.Count);
                    case NotifyCollectionChangedAction.Reset:
                        return true;
                }
            }

            return true; // Implementation defined meaning for the index, so just try to update anyway
        }

        protected abstract bool ShouldUpdate(object sender, PropertyChangedEventArgs e);
    }
}
