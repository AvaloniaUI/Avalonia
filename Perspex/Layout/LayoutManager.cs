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
            if (root != null)
            {
                root.Measure(root.ClientSize);
                root.Arrange(new Rect(root.ClientSize));
            }

            root = null;
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
