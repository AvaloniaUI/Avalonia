// -----------------------------------------------------------------------
// <copyright file="StyleActivator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;

    public enum ActivatorMode
    {
        And,
        Or,
    }

    public class StyleActivator : IObservable<bool>, IDisposable
    {
        private ActivatorMode mode;

        private bool[] values;

        private List<IDisposable> subscriptions = new List<IDisposable>();

        private List<IObserver<bool>> observers = new List<IObserver<bool>>();

        public StyleActivator(
            IList<IObservable<bool>> inputs,
            ActivatorMode mode = ActivatorMode.And)
        {
            int i = 0;

            this.mode = mode;
            this.values = new bool[inputs.Count];

            foreach (IObservable<bool> input in inputs)
            {
                int capturedIndex = i;

                IDisposable subscription = input.Subscribe(
                    x => this.Update(capturedIndex, x),
                    x => this.Finish(capturedIndex),
                    () => this.Finish(capturedIndex));
                this.subscriptions.Add(subscription);
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
            foreach (IObserver<bool> observer in this.observers)
            {
                observer.OnCompleted();
            }

            foreach (IDisposable subscription in this.subscriptions)
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
                this.observers.Add(observer);
                return Disposable.Create(() => this.observers.Remove(observer));
            }
        }

        private void Update(int index, bool value)
        {
            this.values[index] = value;

            bool current;

            switch (this.mode)
            {
                case ActivatorMode.And:
                    current = this.values.All(x => x);
                    break;
                case ActivatorMode.Or:
                    current = this.values.Any(x => x);
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
            var value = this.values[i];
            var unsubscribe =
                (this.values.Length == 1) ||
                (this.mode == ActivatorMode.And ? !value : value);

            if (unsubscribe)
            {
                foreach (IDisposable subscription in this.subscriptions)
                {
                    subscription.Dispose();
                }

                this.HasCompleted = true;
            }
        }

        private void Push(bool value)
        {
            foreach (IObserver<bool> observer in this.observers)
            {
                observer.OnNext(value);
            }
        }
    }
}
