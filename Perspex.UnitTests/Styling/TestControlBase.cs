// -----------------------------------------------------------------------
// <copyright file="SubscribeCheck.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Controls;
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

    public class TestControlBase : IStyleable
    {
        public TestControlBase()
        {
            this.Classes = new Classes();
            this.SubscribeCheckObservable = new TestObservable();
        }

        public string Id { get; set; }

        public Classes Classes { get; set; }

        public TestObservable SubscribeCheckObservable { get; private set; }

        public ITemplatedControl TemplatedParent
        {
            get;
            set;
        }

        public virtual void SetValue(PerspexProperty property, object value, IObservable<bool> activator)
        {
        }
    }

    public static class TestSelectors
    {
        public static Match SubscribeCheck(this Match match)
        {
            return new Match(match)
            {
                Observable = ((TestControlBase)match.Control).SubscribeCheckObservable,
            };
        }
    }
}
