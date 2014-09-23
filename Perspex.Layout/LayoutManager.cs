// -----------------------------------------------------------------------
// <copyright file="LayoutManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Reactive;
    using System.Reactive.Subjects;

    public class LayoutManager : ILayoutManager
    {
        private ILayoutRoot root;

        private Subject<Unit> layoutNeeded;

        public LayoutManager(ILayoutRoot root)
        {
            Contract.Requires<NullReferenceException>(root != null);

            this.root = root;
            this.layoutNeeded = new Subject<Unit>();
        }

        public IObservable<Unit> LayoutNeeded
        {
            get { return this.layoutNeeded; }
        }

        public bool LayoutQueued
        {
            get;
            private set;
        }

        public void ExecuteLayoutPass()
        {
            this.root.Measure(this.root.ClientSize);
            this.root.Arrange(new Rect(this.root.ClientSize));
            this.LayoutQueued = false;
        }

        public void InvalidateMeasure(ILayoutable item)
        {
            if (!this.LayoutQueued)
            {
                IVisual visual = item as IVisual;
                this.layoutNeeded.OnNext(Unit.Default);
                this.LayoutQueued = true;
            }
        }

        public void InvalidateArrange(ILayoutable item)
        {
            if (!this.LayoutQueued)
            {
                IVisual visual = item as IVisual;
                this.layoutNeeded.OnNext(Unit.Default);
                this.LayoutQueued = true;
            }
        }

        public void LayoutFinished()
        {
            this.LayoutQueued = false;
        }
    }
}
