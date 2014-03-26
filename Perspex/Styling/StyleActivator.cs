// -----------------------------------------------------------------------
// <copyright file="Activator.cs" company="Steven Kirk">
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

    public class StyleActivator : IObservable<bool>, IObservableDescription
    {
        ActivatorMode mode;

        List<bool> values = new List<bool>();

        List<IDisposable> subscriptions = new List<IDisposable>();

        List<IObserver<bool>> observers = new List<IObserver<bool>>();

        public StyleActivator(
            IEnumerable<IObservable<bool>> inputs, 
            string description,
            ActivatorMode mode = ActivatorMode.And)
        {
            int i = 0;

            this.Description = description;
            this.mode = mode;

            foreach (IObservable<bool> input in inputs)
            {
                int iCaptured = i;

                this.values.Add(false);

                IDisposable subscription = input.Subscribe(
                    x => this.Update(iCaptured, x),
                    x => this.Finish(iCaptured),
                    () => this.Finish(iCaptured));
                this.subscriptions.Add(subscription);
                ++i;
            }
        }

        public bool CurrentValue
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public bool HasCompleted
        {
            get;
            private set;
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            this.observers.Add(observer);
            observer.OnNext(CurrentValue);
            return Disposable.Create(() => this.observers.Remove(observer));
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

            if (current != CurrentValue)
            {
                this.Push(current);
                CurrentValue = current;
            }
        }

        private void Finish(int i)
        {
            // If the observable has finished on 'false' and we're in And mode then it will never 
            // go back to true so we can unsubscribe from all the other subscriptions now. 
            // Similarly in Or mode; if the completed value is true then we're done.
            bool unsubscribe = this.mode == ActivatorMode.And ? !this.values[i] : this.values[i];

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
