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

    public class Activator : IObservable<bool>
    {
        List<bool> values = new List<bool>();

        List<IDisposable> subscriptions = new List<IDisposable>();

        List<IObserver<bool>> observers = new List<IObserver<bool>>();

        bool last = false;

        public Activator(Match match)
        {
            int i = 0;

            while (match != null)
            {
                int iCaptured = i;

                if (match.Observable != null)
                {
                    this.values.Add(false);

                    IDisposable subscription = match.Observable.Subscribe(
                        x => this.Update(iCaptured, x),
                        x => this.Finish(iCaptured),
                        () => this.Finish(iCaptured));
                    this.subscriptions.Add(subscription);
                    ++i;
                }

                match = match.Previous;
            }
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            this.observers.Add(observer);
            observer.OnNext(last);
            return Disposable.Create(() => this.observers.Remove(observer));
        }

        private void Update(int index, bool value)
        {
            this.values[index] = value;

            bool current = this.values.All(x => x);

            if (current != last)
            {
                this.Push(current);
                last = current;
            }
        }

        private void Finish(int i)
        {
            if (!this.values[i])
            {
                // If the observable has finished on 'false' then it will never go back to true
                // so we can unsubscribe from all the other subscriptions now.
                foreach (IDisposable subscription in this.subscriptions)
                {
                    subscription.Dispose();
                }
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
