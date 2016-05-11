// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Utilities;

namespace Avalonia.Reactive
{
    internal class WeakPropertyChangedObservable : ObservableBase<object>, 
        IWeakSubscriber<AvaloniaPropertyChangedEventArgs>, IDescription
    {
        private WeakReference<IAvaloniaObject> _sourceReference;
        private readonly AvaloniaProperty _property;
        private readonly Subject<object> _changed = new Subject<object>();

        private int _count;

        public WeakPropertyChangedObservable(
            WeakReference<IAvaloniaObject> source, 
            AvaloniaProperty property, 
            string description)
        {
            _sourceReference = source;
            _property = property;
            Description = description;
        }

        public string Description { get; }

        public void OnEvent(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                _changed.OnNext(e.NewValue);
            }
        }

        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            IAvaloniaObject instance;

            if (_sourceReference.TryGetTarget(out instance))
            {
                if (_count++ == 0)
                {
                    WeakSubscriptionManager.Subscribe(
                        instance, 
                        nameof(instance.PropertyChanged), 
                        this);
                }

                observer.OnNext(instance.GetValue(_property));

                return Observable.Using(() => Disposable.Create(DecrementCount), _ => _changed)
                    .Subscribe(observer);
            }
            else
            {
                _changed.OnCompleted();
                observer.OnCompleted();
                return Disposable.Empty;
            }
        }

        private void DecrementCount()
        {
            if (--_count == 0)
            {
                IAvaloniaObject instance;

                if (_sourceReference.TryGetTarget(out instance))
                {
                    WeakSubscriptionManager.Unsubscribe(
                    instance,
                    nameof(instance.PropertyChanged),
                    this);
                }
            }
        }
    }
}
