





namespace Perspex.Styling.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;

    internal class TestSubject<T> : IObserver<T>, IObservable<T>
    {
        private T initial;

        private List<IObserver<T>> subscribers = new List<IObserver<T>>();

        public TestSubject(T initial)
        {
            this.initial = initial;
        }

        public int SubscriberCount
        {
            get { return this.subscribers.Count; }
        }

        public void OnCompleted()
        {
            foreach (IObserver<T> subscriber in this.subscribers.ToArray())
            {
                subscriber.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            foreach (IObserver<T> subscriber in this.subscribers.ToArray())
            {
                subscriber.OnError(error);
            }
        }

        public void OnNext(T value)
        {
            foreach (IObserver<T> subscriber in this.subscribers.ToArray())
            {
                subscriber.OnNext(value);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            this.subscribers.Add(observer);
            observer.OnNext(this.initial);
            return Disposable.Create(() => this.subscribers.Remove(observer));
        }
    }
}
