// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Perspex.Styling
{
    public enum ActivatorMode
    {
        And,
        Or,
    }

    public class StyleActivator : IObservable<bool>, IDisposable
    {
        private ActivatorMode _mode;

        private bool[] _values;

        private List<IDisposable> _subscriptions = new List<IDisposable>();

        private List<IObserver<bool>> _observers = new List<IObserver<bool>>();

        public StyleActivator(
            IList<IObservable<bool>> inputs,
            ActivatorMode mode = ActivatorMode.And)
        {
            int i = 0;

            _mode = mode;
            _values = new bool[inputs.Count];

            foreach (IObservable<bool> input in inputs)
            {
                int capturedIndex = i;

                IDisposable subscription = input.Subscribe(
                    x => this.Update(capturedIndex, x),
                    x => this.Finish(capturedIndex),
                    () => this.Finish(capturedIndex));
                _subscriptions.Add(subscription);
                ++i;
            }
        }

        public bool CurrentValue
        {
            get;
            private set;
        }

        public bool HasCompleted
        {
            get;
            private set;
        }

        public void Dispose()
        {
            foreach (IObserver<bool> observer in _observers)
            {
                observer.OnCompleted();
            }

            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            observer.OnNext(this.CurrentValue);

            if (this.HasCompleted)
            {
                observer.OnCompleted();
                return Disposable.Empty;
            }
            else
            {
                _observers.Add(observer);
                return Disposable.Create(() => _observers.Remove(observer));
            }
        }

        private void Update(int index, bool value)
        {
            _values[index] = value;

            bool current;

            switch (_mode)
            {
                case ActivatorMode.And:
                    current = _values.All(x => x);
                    break;
                case ActivatorMode.Or:
                    current = _values.Any(x => x);
                    break;
                default:
                    throw new InvalidOperationException("Invalid Activator mode.");
            }

            if (current != this.CurrentValue)
            {
                this.Push(current);
                this.CurrentValue = current;
            }
        }

        private void Finish(int i)
        {
            // We can unsubscribe from everything if the completed observable:
            // - Is the only subscription.
            // - Has finished on 'false' and we're in And mode
            // - Has finished on 'true' and we're in Or mode
            var value = _values[i];
            var unsubscribe =
                (_values.Length == 1) ||
                (_mode == ActivatorMode.And ? !value : value);

            if (unsubscribe)
            {
                foreach (IDisposable subscription in _subscriptions)
                {
                    subscription.Dispose();
                }

                this.HasCompleted = true;
            }
        }

        private void Push(bool value)
        {
            foreach (IObserver<bool> observer in _observers)
            {
                observer.OnNext(value);
            }
        }
    }
}
