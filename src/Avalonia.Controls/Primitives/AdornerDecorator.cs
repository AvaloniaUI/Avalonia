// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    public class AdornerDecorator : Decorator
    {
        public AdornerDecorator()
        {
            AdornerLayer = new AdornerLayer();
            ((ISetLogicalParent)AdornerLayer).SetParent(this);
            AdornerLayer.ZIndex = int.MaxValue;
            VisualChildren.Add(AdornerLayer);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            ((ILogical)AdornerLayer).NotifyAttachedToLogicalTree(e);
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
