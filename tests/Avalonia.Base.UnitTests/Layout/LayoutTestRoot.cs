using System;
using Avalonia.Layout;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Layout
{
    internal class LayoutTestRoot : TestRoot
    {
        public bool Measured { get; set; }
        public bool Arranged { get; set; }
        public Func<Layoutable, Size, Size> DoMeasureOverride { get; set; }
        public Func<Layoutable, Size, Size> DoArrangeOverride { get; set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            Measured = true;
            return DoMeasureOverride != null ?
                DoMeasureOverride(this, availableSize) :
                base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Arranged = true;
            return DoArrangeOverride != null ?
                DoArrangeOverride(this, finalSize) :
                base.ArrangeOverride(finalSize);
        }
    }
}
