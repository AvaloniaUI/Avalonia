using System;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Base.UnitTests.Layout
{
    internal class LayoutTestControl : Decorator
    {
        public bool Measured { get; set; }
        public bool Arranged { get; set; }
        public Func<Layoutable, Size, Size> DoMeasureOverride { get; set; }
        public Func<Layoutable, Size, Size> DoArrangeOverride { get; set; }
        public bool CallBaseMeasure { get; set; }
        public bool CallBaseArrange { get; set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            Measured = true;

            if (DoMeasureOverride is not null)
            {
                var overrideResult = DoMeasureOverride(this, availableSize);
                return CallBaseMeasure ?
                    base.MeasureOverride(overrideResult) :
                    overrideResult;
            }
            else
            {
                return base.MeasureOverride(availableSize);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Arranged = true;

            if (DoArrangeOverride is not null)
            {
                var overrideResult = DoArrangeOverride(this, finalSize);
                return CallBaseArrange ?
                    base.ArrangeOverride(overrideResult) :
                    overrideResult;
            }
            else
            {
                return base.ArrangeOverride(finalSize);
            }
        }
    }
}
