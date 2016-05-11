// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;

namespace Avalonia.Reactive
{
    /// <summary>
    /// Provides common observable methods not found in standard Rx framework.
    /// </summary>
    public static class ObservableEx
    {
        /// <summary>
        /// Returns an observable that fires once with the specified value and never completes.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The observable.</returns>
        public static IObservable<T> SingleValue<T>(T value)
        {
            return new SingleValueImpl<T>(value);
        }

        private class SingleValueImpl<T> : IObservable<T>
        {
            private T _value;

            public SingleValueImpl(T value)
            {
                _value = value;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                observer.OnNext(_value);
                return Disposable.Empty;
            }
        }
    }
}
