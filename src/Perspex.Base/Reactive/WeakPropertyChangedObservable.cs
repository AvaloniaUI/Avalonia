// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Utilities;

namespace Perspex.Reactive
{
    internal class WeakPropertyChangedObservable : ObservableBase<object>, 
        IWeakSubscriber<PerspexPropertyChangedEventArgs>, IDescription
    {
        private WeakReference<IPerspexObject> _sourceReference;
        private readonly PerspexProperty _property;
        private readonly Subject<object> _changed = new Subject<object>();

        private int _count;

        public WeakPropertyChangedObservable(
            WeakReference<IPerspexObject> source, 
            PerspexProperty property, 
            string description)
        {
            _sourceReference = source;
            _property = property;
            Description = description;
        }

        public string Description { get; }

        public void OnEvent(object sender, PerspexPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                _changed.OnNext(e.NewValue);
            }
        }

        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            IPerspexObject instance;

            if (_sourceReference.TryGetTarget(out instance))
            {
                if (_count == 0)
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
                IPerspexObject instance;

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
