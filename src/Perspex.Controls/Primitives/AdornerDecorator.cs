// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls.Primitives
{
    public class AdornerDecorator : Decorator
    {
        public AdornerDecorator()
        {
            this.AdornerLayer = new AdornerLayer();
            ((ISetLogicalParent)this.AdornerLayer).SetParent(this);
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
