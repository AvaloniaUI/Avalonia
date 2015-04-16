// -----------------------------------------------------------------------
// <copyright file="AdornerLayer.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.VisualTree;

    // TODO: Need to track position of adorned elements and move the adorner if they move.
    public class AdornerLayer : Panel
    {
        public static PerspexProperty<Visual> AdornedElementProperty =
            PerspexProperty.RegisterAttached<AdornerLayer, Visual, Visual>("AdornedElement");

        static AdornerLayer()
        {
            IsHitTestVisibleProperty.OverrideDefaultValue(typeof(AdornerLayer), false);
        }

        public static Visual GetAdornedElement(Visual adorner)
        {
            return adorner.GetValue(AdornedElementProperty);
        }

        public static void SetAdornedElement(Visual adorner, Visual adorned)
        {
            adorner.SetValue(AdornedElementProperty, adorned);
        }

        public static AdornerLayer GetAdornerLayer(IVisual visual)
        {
            return visual.GetVisualAncestors()
                .OfType<AdornerDecorator>()
                .FirstOrDefault()
                ?.AdornerLayer;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var parent = this.Parent;

            foreach (var child in this.Children)
            {
                var adorned = GetAdornedElement(child);

                if (adorned != null)
                {
                    var transform = adorned.TransformToVisual(parent);
                    var position = new Point(0, 0) * transform;
                    child.Arrange(new Rect(position, adorned.Bounds.Size));
                }
                else
                {
                    child.Arrange(new Rect(child.DesiredSize.Value));
                }
            }

            return finalSize;
        }

        protected override void OnChildrenAdded(IEnumerable<Control> child)
        {
            this.InvalidateArrange();
        }

        protected override void OnChildrenRemoved(IEnumerable<Control> child)
        {
            this.InvalidateArrange();
        }
    }
}
