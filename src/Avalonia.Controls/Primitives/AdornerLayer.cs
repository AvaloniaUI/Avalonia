using System;
using System.Collections.Specialized;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a surface for showing adorners.
    /// Adorners are always on top of the adorned element and are positioned to stay relative to the adorned element.
    /// </summary>
    /// <remarks>
    /// TODO: Need to track position of adorned elements and move the adorner if they move.
    /// </remarks>
    public class AdornerLayer : Canvas, ICustomSimpleHitTest
    {
        /// <summary>
        /// Allows for getting and setting of the adorned element.
        /// </summary>
        public static readonly AttachedProperty<Visual?> AdornedElementProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, Visual?>("AdornedElement");

        /// <summary>
        /// Allows for controlling clipping of the adorner.
        /// </summary>
        public static readonly AttachedProperty<bool> IsClipEnabledProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, bool>("IsClipEnabled", true);

        /// <summary>
        /// Allows for getting and setting of the adorner for control.
        /// </summary>
        public static readonly AttachedProperty<Control?> AdornerProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, Control?>("Adorner");

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

        public static Visual? GetAdornedElement(Visual adorner)
        {
            return adorner.GetValue(AdornedElementProperty);
        }

        public static void SetAdornedElement(Visual adorner, Visual adorned)
        {
            adorner.SetValue(AdornedElementProperty, adorned);
        }

        public static AdornerLayer? GetAdornerLayer(IVisual visual)
        {
            return visual.FindAncestorOfType<VisualLayerManager>()?.AdornerLayer;
        }

        public static bool GetIsClipEnabled(Visual adorner)
        {
            return adorner.GetValue(IsClipEnabledProperty);
        }

        public static void SetIsClipEnabled(Visual adorner, bool isClipEnabled)
        {
            adorner.SetValue(IsClipEnabledProperty, isClipEnabled);
        }

        public static Control? GetAdorner(Visual visual)
        {
            return visual.GetValue(AdornerProperty);
        }

        public static void SetAdorner(Visual visual, Control? adorner)
        {
            visual.SetValue(AdornerProperty, adorner);

            SetVisualAdorner(visual, adorner);
        }

        private static void SetVisualAdorner(Visual visual, Control? adorner)
        {
            var layer = default(AdornerLayer);

            visual.AttachedToVisualTree += (_, _) =>
            {
                layer = AddVisualAdorner(visual, adorner);
            };

            visual.DetachedFromVisualTree += (_, _) =>
            {
                RemoveVisualAdorner(visual, adorner, layer);
            };
        }

        private static AdornerLayer? AddVisualAdorner(Visual visual, Control? adorner)
        {
            if (adorner is null)
            {
                return null;
            }

            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (layer == null || layer.Children.Contains(adorner))
            {
                return layer;
            }

            AdornerLayer.SetAdornedElement(adorner, visual);
            AdornerLayer.SetIsClipEnabled(adorner, false);

            ((ISetLogicalParent) adorner).SetParent(visual);
            layer.Children.Add(adorner);

            return layer;
        }

        private static void RemoveVisualAdorner(Visual visual, Control? adorner, AdornerLayer? layer)
        {
            if (adorner is null)
            {
                return;
            }

            // var layer = AdornerLayer.GetAdornerLayer(visual);
            if (layer is null || !layer.Children.Contains(adorner))
            {
                return;
            }

            layer.Children.Remove(adorner);
            ((ISetLogicalParent) adorner).SetParent(null);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
            {
                if (child is AvaloniaObject ao)
                {
                    var info = ao.GetValue(s_adornedElementInfoProperty);

                    if (info != null && info.Bounds.HasValue)
                    {
                        child.Measure(info.Bounds.Value.Bounds.Size);
                    }
                    else
                    {
                        child.Measure(availableSize);
                    }
                }
            }

            return default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children)
            {
                if (child is AvaloniaObject ao)
                {
                    var info = ao.GetValue(s_adornedElementInfoProperty);
                    var isClipEnabled = ao.GetValue(IsClipEnabledProperty);

                    if (info != null && info.Bounds.HasValue)
                    {
                        child.RenderTransform = new MatrixTransform(info.Bounds.Value.Transform);
                        child.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Absolute);
                        UpdateClip(child, info.Bounds.Value, isClipEnabled);
                        child.Arrange(info.Bounds.Value.Bounds);
                    }
                    else
                    {
                        ArrangeChild((Control) child, finalSize);
                    }
                }
            }

            return finalSize;
        }

        private static void AdornedElementChanged(AvaloniaPropertyChangedEventArgs<Visual?> e)
        {
            var adorner = (Visual)e.Sender;
            var adorned = e.NewValue.GetValueOrDefault();
            var layer = adorner.GetVisualParent<AdornerLayer>();
            layer?.UpdateAdornedElement(adorner, adorned);
        }

        private void UpdateClip(IControl control, TransformedBounds bounds, bool isEnabled)
        {
            if (!isEnabled)
            {
                control.Clip = null;

                return;
            }

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

        private void ChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Visual i in e.NewItems!)
                    {
                        UpdateAdornedElement(i, i.GetValue(AdornedElementProperty));
                    }

                    break;
            }

            InvalidateArrange();
        }

        private void UpdateAdornedElement(Visual adorner, Visual? adorned)
        {
            if (adorner.CompositionVisual != null)
                adorner.CompositionVisual.AdornedVisual = adorned?.CompositionVisual;
            
            var info = adorner.GetValue(s_adornedElementInfoProperty);

            if (info != null)
            {
                info.Subscription!.Dispose();

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

                if (adorner.CompositionVisual != null)
                    info.Subscription = adorned.GetObservable(BoundsProperty).Subscribe(x =>
                    {
                        info.Bounds = new TransformedBounds(new Rect(adorned.Bounds.Size), new Rect(adorned.Bounds.Size), Matrix.Identity);
                        InvalidateMeasure();
                    });
                else
                    info.Subscription = adorned.GetObservable(TransformedBoundsProperty).Subscribe(x =>
                    {
                        info.Bounds = x;
                        InvalidateMeasure();
                    });
            }
        }

        public bool HitTest(Point point) => Children.HitTestCustom(point);

        private class AdornedElementInfo
        {
            public IDisposable? Subscription { get; set; }

            public TransformedBounds? Bounds { get; set; }
        }
    }
}
