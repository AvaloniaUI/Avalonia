using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayLayer : Canvas, ICustomSimpleHitTest
    {
        private readonly List<Popup> _registeredPopups = new List<Popup>();

        public Size AvailableSize { get; private set; }
        public static OverlayLayer? GetOverlayLayer(IVisual visual)
        {
            foreach(var v in visual.GetVisualAncestors())
                if(v is VisualLayerManager vlm)
                    if (vlm.OverlayLayer != null)
                        return vlm.OverlayLayer;
            if (visual is TopLevel tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.OverlayLayer;
            }

            return null;
        }

        public bool HitTest(Point point) => Children.HitTestCustom(point);

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (Control child in Children)
                child.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // We are saving it here since child controls might need to know the entire size of the overlay
            // and Bounds won't be updated in time
            AvailableSize = finalSize;
            return base.ArrangeOverride(finalSize);
        }

        public void RegisterWindowedPopup(Popup p)
        {
            _registeredPopups.Add(p);
            Debug.WriteLine($"Popup registered; total {_registeredPopups.Count}");
        }

        public void UnregisterWindowedPopup(Popup p)
        {
            _registeredPopups.Remove(p);
            Debug.WriteLine($"Popup Unregistered; total {_registeredPopups.Count}");
        }

        public IInputElement? GetTopMostLightDismissElement()
        {
            // Windowed popups take priority since they're 'above' the window
            // List is in order the popup was shown, so last item should be
            // most recent popup opened
            if (_registeredPopups.Count > 0)
            {
                var ct = _registeredPopups.Count;
                for (int i = ct - 1; i >= 0; i--)
                {
                    if(_registeredPopups[i].IsLightDismissEnabled)
                    {
                        return _registeredPopups[i];
                    }    
                }
            }

            var childrenCount = Children.Count;
            if (childrenCount > 0)
            {
                // TODO: For Overlay popups, we don't currently have a way to get the actual popup
                // so we treat all as light dismiss. Custom controls (third party dialogs, for ex)
                // should also be treated as light dismiss here
                // For now, just returning the "top most" (excluding z-order) child

                return Children[childrenCount - 1];
            }

            return null;
        }
    }
}
