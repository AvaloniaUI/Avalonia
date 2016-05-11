// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Avalonia.Reactive
{
    public class AnonymousSubject<T, U> : ISubject<T, U>
    {
        private readonly IObserver<T> _observer;
        private readonly IObservable<U> _observable;

        public AnonymousSubject(IObserver<T> observer, IObservable<U> observable)
        {
            _observer = observer;
            _observable = observable;
        }

        public void OnCompleted()
        {
            _observer.OnCompleted();
        }

        public void OnError(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            _observer.OnError(error);
        }

        public void OnNext(T value)
        {
            _observer.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<U> observer)
        {
            if (observer == null)
                throw new ArgumentNullException("observer");

            //
            // [OK] Use of unsafe Subscribe: non-pretentious wrapping of an observable sequence.
            //
            return _observable.Subscribe/*Unsafe*/(observer);
        }
    }
}
