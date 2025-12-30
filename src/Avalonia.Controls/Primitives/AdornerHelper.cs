using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives;

class AdornerHelper
{

    public static IDisposable SubscribeToAncestorPropertyChanges(Visual visual, 
        bool includeClip, Action changed)
    {
        return new AncestorPropertyChangesSubscription(visual, includeClip, changed);
    }

    private class AncestorPropertyChangesSubscription : IDisposable
    {
        private readonly Visual _visual;
        private readonly bool _includeClip;
        private readonly Action _changed;
        private readonly EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChangedHandler;
        private readonly List<Visual> _subscriptions = new List<Visual>();
        private bool _isDisposed;

        public AncestorPropertyChangesSubscription(Visual visual, bool includeClip, Action changed)
        {
            _visual = visual;
            _includeClip = includeClip;
            _changed = changed;
            _propertyChangedHandler = OnPropertyChanged;

            _visual.AttachedToVisualTree += OnAttachedToVisualTree;
            _visual.DetachedFromVisualTree += OnDetachedFromVisualTree;

            if (_visual.IsAttachedToVisualTree)
            {
                SubscribeToAncestors();
            }
        }

        private void SubscribeToAncestors()
        {
            UnsubscribeFromAncestors();

            // Subscribe to the visual's own Bounds property
            _visual.PropertyChanged += _propertyChangedHandler;
            _subscriptions.Add(_visual);

            // Walk up the ancestor chain
            var ancestor = _visual.VisualParent;
            while (ancestor != null)
            {
                if (ancestor is Visual visualAncestor)
                {
                    visualAncestor.PropertyChanged += _propertyChangedHandler;
                    _subscriptions.Add(visualAncestor);
                }
                ancestor = ancestor.VisualParent;
            }
        }

        private void UnsubscribeFromAncestors()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.PropertyChanged -= _propertyChangedHandler;
            }
            _subscriptions.Clear();
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (!e.IsEffectiveValueChange)
                return;

            bool shouldNotify = false;

            if (e.Property == Visual.RenderTransformProperty || e.Property == Visual.BoundsProperty)
            {
                shouldNotify = true;
            }
            else if (_includeClip)
            {
                if (e.Property == Visual.ClipToBoundsProperty ||
                    e.Property == Visual.ClipProperty) shouldNotify = true;
            }
            
            if (shouldNotify)
            {
                _changed();
            }
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            SubscribeToAncestors();
            _changed();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            UnsubscribeFromAncestors();
            _changed();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            UnsubscribeFromAncestors();
            _visual.AttachedToVisualTree -= OnAttachedToVisualTree;
            _visual.DetachedFromVisualTree -= OnDetachedFromVisualTree;
        }
    }

    public static Geometry? CalculateAdornerClip(Visual adornedElement)
    {
        // Walk ancestor stack and calculate clip geometry relative to the current visual.
        // If ClipToBounds = true, add extra RectangleGeometry for Bounds.Size
        
        Geometry? result = null;
        var ancestor = adornedElement.VisualParent;

        while (ancestor != null)
        {
            if (ancestor is Visual visualAncestor)
            {
                Geometry? ancestorClip = null;

                // Check if ancestor has ClipToBounds enabled
                if (visualAncestor.ClipToBounds)
                {
                    ancestorClip = new RectangleGeometry(new Rect(visualAncestor.Bounds.Size));
                }

                // Check if ancestor has explicit Clip geometry
                if (visualAncestor.Clip != null)
                {
                    if (ancestorClip != null)
                    {
                        ancestorClip = new CombinedGeometry(GeometryCombineMode.Intersect, ancestorClip, visualAncestor.Clip);
                    }
                    else
                    {
                        ancestorClip = visualAncestor.Clip;
                    }
                }

                // Transform the clip geometry to adorned element's coordinate space
                if (ancestorClip != null)
                {
                    var transform = visualAncestor.TransformToVisual(adornedElement);
                    if (transform.HasValue && !transform.Value.IsIdentity)
                    {
                        ancestorClip = ancestorClip.Clone();
                        ancestorClip.Transform = new MatrixTransform(transform.Value);
                    }

                    // Combine with existing result
                    if (result != null)
                    {
                        result = new CombinedGeometry(GeometryCombineMode.Intersect, result, ancestorClip);
                    }
                    else
                    {
                        result = ancestorClip;
                    }
                }
            }

            ancestor = ancestor.VisualParent;
        }

        return result;
    }
    
}