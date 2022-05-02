using System;
using System.Reactive.Subjects;

namespace Avalonia.Reactive
{
    public class SingleSubscriberSubject<T> : SingleSubscriberObservableBase<T>, ISubject<T>
    {
        public void OnCompleted()
        {
            PublishCompleted();
        }

        public void OnError(Exception error)
        {
            PublishError(error);
        }

        public void OnNext(T value)
        {
            PublishNext(value);
        }

        protected override void Unsubscribed()
        {
        }

        protected override void Subscribed()
        {
        }
    }
}
