using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    public abstract class VirtualizingLayout : Layout
    {
        public sealed override void InitializeForContext(LayoutContext context)
        {
            InitializeForContextCore((VirtualizingLayoutContext)context);
        }

        public sealed override void UninitializeForContext(LayoutContext context)
        {
            UninitializeForContextCore((VirtualizingLayoutContext)context);
        }

        public sealed override Size Measure(LayoutContext context, Size availableSize)
        {
            return MeasureOverride((VirtualizingLayoutContext)context, availableSize);
        }

        public sealed override Size Arrange(LayoutContext context, Size finalSize)
        {
            return ArrangeOverride((VirtualizingLayoutContext)context, finalSize);
        }

        protected virtual void InitializeForContextCore(VirtualizingLayoutContext context)
        {
        }

        protected virtual void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
        }

        protected abstract Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize);

        protected virtual Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize) => finalSize;

        protected internal virtual void OnItemsChangedCore(
            VirtualizingLayoutContext context,
            object source,
            NotifyCollectionChangedEventArgs args) => InvalidateMeasure();
    }
}
