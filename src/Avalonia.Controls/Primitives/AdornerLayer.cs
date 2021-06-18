using System;
using System.Collections.Specialized;
using System.Linq;
using System.Resources;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    // TODO: Need to track position of adorned elements and move the adorner if they move.
    public class AdornerLayer : Canvas, ICustomSimpleHitTest
    {
        public static readonly AttachedProperty<Visual> AdornedElementProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, Visual>("AdornedElement");

        private static readonly AttachedProperty<AdornedElementInfo> s_adornedElementInfoProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, AdornedElementInfo>("AdornedElementInfo");

        static AdornerLayer()
        {
            AdornedElementProperty.Changed.Subscribe(AdornedElementChanged);
        }

        public AdornerLayer()
        {
            Children.CollectionChanged += ChildrenCollectionChanged;
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
                .OfType<VisualLayerManager>()
                .FirstOrDefault()
                ?.AdornerLayer;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
            {
                var info = child.GetValue(s_adornedElementInfoProperty);

                if (info != null && info.Bounds.HasValue)
                {
                    child.Measure(info.Bounds.Value.Bounds.Size);
                }
                else
                {
                    child.Measure(availableSize);
                }
            }

            return default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children)
            {
                var info = child.GetValue(s_adornedElementInfoProperty);

                if (info != null && info.Bounds.HasValue)
                {
                    child.RenderTransform = new MatrixTransform(info.Bounds.Value.Transform);
                    child.RenderTransformOrigin = new RelativePoint(new Point(0,0), RelativeUnit.Absolute);
                    UpdateClip(child, info.Bounds.Value);
                    child.Arrange(info.Bounds.Value.Bounds);
                }
                else
                {
                    child.Arrange(new Rect(finalSize));
                }
            }

            return finalSize;
        }

        private static void AdornedElementChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var adorner = (Visual)e.Sender;
            var adorned = (Visual)e.NewValue;
            var layer = adorner.GetVisualParent<AdornerLayer>();
            layer?.UpdateAdornedElement(adorner, adorned);
        }

        private void UpdateClip(IControl control, TransformedBounds bounds)
        {
            if (!(control.Clip is RectangleGeometry clip))
            {
                clip = new RectangleGeometry();
                control.Clip = clip;
            }

            var clipBounds = bounds.Bounds;

            if (bounds.Transform.HasInverse)
            {
                clipBounds = bounds.Clip.TransformToAABB(bounds.Transform.Invert());
            }

            clip.Rect = clipBounds;
        }

        private void ChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Visual i in e.NewItems)
                    {
                        UpdateAdornedElement(i, i.GetValue(AdornedElementProperty));
                    }

                    break;
            }

            InvalidateArrange();
        }

        private void UpdateAdornedElement(Visual adorner, Visual adorned)
        {
            var info = adorner.GetValue(s_adornedElementInfoProperty);

            if (info != null)
            {
                info.Subscription.Dispose();

                if (adorned == null)
                {
                    adorner.ClearValue(s_adornedElementInfoProperty);
                }
            }

            if (adorned != null)
            {
                if (info == null)
                {
                    info = new AdornedElementInfo();
                    adorner.SetValue(s_adornedElementInfoProperty, info);
                }

                info.Subscription = adorned.GetObservable(TransformedBoundsProperty).Subscribe(x =>
                {
                    info.Bounds = x;
                    InvalidateArrange();
                });
            }
        }

        public bool HitTest(Point point) => Children.HitTestCustom(point);

        private class AdornedElementInfo
        {
            public IDisposable Subscription { get; set; }

            public TransformedBounds? Bounds { get; set; }
        }
    }
}
