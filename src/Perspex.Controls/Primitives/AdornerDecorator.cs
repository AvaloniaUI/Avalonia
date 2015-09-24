// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls.Primitives
{
    public class AdornerDecorator : Decorator
    {
        public AdornerDecorator()
        {
            AdornerLayer = new AdornerLayer();
            ((ISetLogicalParent)AdornerLayer).SetParent(this);
            AdornerLayer.ZIndex = int.MaxValue;
            AddVisualChild(AdornerLayer);
        }

        public AdornerLayer AdornerLayer
        {
            get;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            AdornerLayer.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            AdornerLayer.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }
    }
}
