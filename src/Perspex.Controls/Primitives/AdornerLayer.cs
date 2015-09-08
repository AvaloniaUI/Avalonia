





namespace Perspex.Controls.Primitives
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using Perspex.VisualTree;

    // TODO: Need to track position of adorned elements and move the adorner if they move.
    public class AdornerLayer : Panel
    {
        public static PerspexProperty<Visual> AdornedElementProperty =
            PerspexProperty.RegisterAttached<AdornerLayer, Visual, Visual>("AdornedElement");

        private static PerspexProperty<AdornedElementInfo> AdornedElementInfoProperty =
            PerspexProperty.RegisterAttached<AdornerLayer, Visual, AdornedElementInfo>("AdornedElementInfo");

        private BoundsTracker tracker = new BoundsTracker();

        static AdornerLayer()
        {
            AdornedElementProperty.Changed.Subscribe(AdornedElementChanged);
            IsHitTestVisibleProperty.OverrideDefaultValue(typeof(AdornerLayer), false);
        }

        public AdornerLayer()
        {
            this.Children.CollectionChanged += this.ChildrenCollectionChanged;
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
                var info = (AdornedElementInfo)child.GetValue(AdornedElementInfoProperty);

                if (info != null)
                {
                    child.Arrange(info.Bounds.Bounds);
                }
                else
                {
                    child.Arrange(new Rect(child.DesiredSize));
                }
            }

            return finalSize;
        }

        private static void AdornedElementChanged(PerspexPropertyChangedEventArgs e)
        {
            var adorner = (Visual)e.Sender;
            var adorned = (Visual)e.NewValue;
            var layer = adorner.GetVisualParent<AdornerLayer>();

            if (layer != null)
            {
                layer.UpdateAdornedElement(adorner, adorned);
            }
        }

        private void ChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Visual i in e.NewItems)
                    {
                        this.UpdateAdornedElement(i, i.GetValue(AdornedElementProperty));
                    }

                    break;
            }

            this.InvalidateArrange();
        }

        private void UpdateAdornedElement(Visual adorner, Visual adorned)
        {
            var info = adorner.GetValue(AdornedElementInfoProperty);

            if (info != null)
            {
                info.Subscription.Dispose();

                if (adorned == null)
                {
                    adorner.ClearValue(AdornedElementInfoProperty);
                }
            }

            if (adorned != null)
            {
                if (info == null)
                {
                    info = new AdornedElementInfo();
                    adorner.SetValue(AdornedElementInfoProperty, info);
                }

                info.Subscription = this.tracker.Track(adorned).Subscribe(x =>
                {
                    info.Bounds = x;
                    this.InvalidateArrange();
                });
            }
        }

        private class AdornedElementInfo
        {
            public IDisposable Subscription { get; set; }

            public TransformedBounds Bounds { get; set; }
        }
    }
}
