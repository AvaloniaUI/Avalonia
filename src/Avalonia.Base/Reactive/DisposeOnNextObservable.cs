using System;
using Avalonia.Threading;

namespace Avalonia.Reactive
{
    public class DisposeOnNextObservable<T> : LightweightObservableBase<T>, IObserver<T> where T : IDisposable
    {
        private IDisposable lastValue;

        private void ValueNext(T value)
        {
            lastValue?.Dispose();
            lastValue = value;
            this.PublishNext(value);
        }

        public void OnCompleted()
        {
            this.PublishCompleted();
        }

        public void OnError(Exception error)
        {
            this.PublishError(error);
        }

        void IObserver<T>.OnNext(T value)
        {
            ValueNext(value);
        }
 
        protected override void Initialize()
        {
        }

        protected override void Deinitialize()
        {
        }
    }
}