// -----------------------------------------------------------------------
// <copyright file="LayoutManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Linq;
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

        public void ExecuteLayoutPass()
        {
            if (this.root != null)
            {
                this.root.Measure(this.root.ClientSize);
                this.root.Arrange(new Rect(this.root.ClientSize));
            }

            this.root = null;
        }

        public void InvalidateMeasure(ILayoutable item)
        {
            IVisual visual = item as IVisual;
            this.layoutNeeded.OnNext(Unit.Default);
        }

        public void InvalidateArrange(ILayoutable item)
        {
            IVisual visual = item as IVisual;
            this.layoutNeeded.OnNext(Unit.Default);
        }
    }
}
