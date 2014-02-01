// -----------------------------------------------------------------------
// <copyright file="LayoutManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Subjects;
    using Perspex.Controls;

    public class LayoutManager : ILayoutManager
    {
        private ILayoutRoot root;

        private Subject<Unit> layoutNeeded;

        public LayoutManager()
        {
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
            this.root = item.GetLayoutRoot();
            this.layoutNeeded.OnNext(Unit.Default);
        }

        public void InvalidateArrange(ILayoutable item)
        {
            this.root = item.GetLayoutRoot();
            this.layoutNeeded.OnNext(Unit.Default);
        }
    }
}
