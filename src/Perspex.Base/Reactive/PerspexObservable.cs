// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Disposables;

namespace Perspex.Reactive
{
    /// <summary>
    /// An <see cref="IObservable{T}"/> with an additional description.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    public sealed class PerspexObservable<T> : ObservableBase<T>, IDescription
    {
        private readonly Func<IObserver<T>, IDisposable> _subscribe;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexObservable{T}"/> class.
        /// </summary>
        /// <param name="subscribe">The subscribe function.</param>
        /// <param name="description">The description of the observable.</param>
        public PerspexObservable(Func<IObserver<T>, IDisposable> subscribe, string description)
        {
            if (subscribe == null)
            {
                throw new ArgumentNullException("subscribe");
            }

            _subscribe = subscribe;
            Description = description;
        }

        /// <summary>
        /// Gets the description of the observable.
        /// </summary>
        public string Description { get; }

        /// <inheritdoc/>
        protected override IDisposable SubscribeCore(IObserver<T> observer)
        {
            return _subscribe(observer) ?? Disposable.Empty;
        }
    }
}