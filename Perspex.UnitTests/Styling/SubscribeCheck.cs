// -----------------------------------------------------------------------
// <copyright file="SubscribeCheck.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Styling;
    using Match = Perspex.Styling.Match;

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

    public class SubscribeCheck : IStyleable
    {
        public SubscribeCheck()
        {
            this.Classes = Classes;
            this.SubscribeCheckObservable = new TestObservable();
        }

        public Classes Classes
        {
            get;
            private set;
        }

        public TestObservable SubscribeCheckObservable
        {
            get;
            private set;
        }

        public virtual void SetValue(PerspexProperty property, object value, IObservable<bool> activator)
        {
        }
    }

    public static class TestSelectors
    {
        public static Match SubscribeCheck(this Match match)
        {
            match.Observables.Add(((SubscribeCheck)match.Control).SubscribeCheckObservable);
            return match;
        }
    }
}
