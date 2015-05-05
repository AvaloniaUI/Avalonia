// -----------------------------------------------------------------------
// <copyright file="PerspexObservable.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Reactive
{
    using System;
    using System.Reactive;
    using System.Reactive.Disposables;

    public sealed class PerspexObservable<T> : ObservableBase<T>, IDescription
    {
        private readonly Func<IObserver<T>, IDisposable> subscribe;

        public PerspexObservable(Func<IObserver<T>, IDisposable> subscribe, string description)
        {
            if (subscribe == null)
            {
                throw new ArgumentNullException("subscribe");
            }

            this.subscribe = subscribe;
            this.Description = description;
        }

        public string Description { get; }

        protected override IDisposable SubscribeCore(IObserver<T> observer)
        {
            return this.subscribe(observer) ?? Disposable.Empty;
        }
    }
}