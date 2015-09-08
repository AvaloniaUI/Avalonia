





namespace Perspex.Styling.UnitTests
{
    using System;
    using System.Reactive.Disposables;

    public class TestObservable : IObservable<bool>
    {
        public int SubscribedCount { get; private set; }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            ++this.SubscribedCount;
            observer.OnNext(true);
            return Disposable.Create(() => --this.SubscribedCount);
        }
    }
}
