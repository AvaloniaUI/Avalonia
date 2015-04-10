// -----------------------------------------------------------------------
// <copyright file="AdornerDecorator.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    public class AdornerDecorator : Decorator
    {
        public AdornerDecorator()
        {
            this.AdornerLayer = new AdornerLayer();
            this.AdornerLayer.Parent = this;
            this.AdornerLayer.ZIndex = int.MaxValue;
            this.AddVisualChild(this.AdornerLayer);
        }

        public AdornerLayer AdornerLayer
        {
            get;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            this.AdornerLayer.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            this.AdornerLayer.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }
    }
}
