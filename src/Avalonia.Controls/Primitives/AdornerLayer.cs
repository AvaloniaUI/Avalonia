// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.VisualTree;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    // TODO: Need to track position of adorned elements and move the adorner if they move.
    public class AdornerLayer : Panel
    {
        public static AttachedProperty<Visual> AdornedElementProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, Visual>("AdornedElement");

        private static readonly AttachedProperty<AdornedElementInfo> s_adornedElementInfoProperty =
            AvaloniaProperty.RegisterAttached<AdornerLayer, Visual, AdornedElementInfo>("AdornedElementInfo");

        private readonly BoundsTracker _tracker = new BoundsTracker();

        static AdornerLayer()
        {
            AdornedElementProperty.Changed.Subscribe(AdornedElementChanged);
            IsHitTestVisibleProperty.OverrideDefaultValue(typeof(AdornerLayer), false);
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
                .OfType<AdornerDecorator>()
                .FirstOrDefault()
                ?.AdornerLayer;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var parent = Parent;

            foreach (var child in Children)
            {
                var info = (AdornedElementInfo)child.GetValue(s_adornedElementInfoProperty);

                if (info != null)
                {
                    child.RenderTransform = new MatrixTransform(info.Bounds.Transform);
                    child.TransformOrigin = new RelativePoint(new Point(0,0), RelativeUnit.Absolute);
                    child.Arrange(info.Bounds.Bounds);
                }
                else
                {
                    child.Arrange(new Rect(child.DesiredSize));
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

                info.Subscription = _tracker.Track(adorned).Subscribe(x =>
                {
                    info.Bounds = x;
                    InvalidateArrange();
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
