using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayLayer : Canvas, ICustomSimpleHitTest, IOverlayHost
    {
        private readonly List<OverlayRegistrationInfo> _registeredPopups = new List<OverlayRegistrationInfo>();

        static OverlayLayer()
        {
            // Explicitly disable focus from entering the overlay layer.
            // Focus manager will manually search this and enforce "Cycle" behavior, but we don't want
            // tabbing from main window content to suddenly end up in an overlay
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<OverlayLayer>(KeyboardNavigationMode.None);
        }

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

        public void RegisterOverlay(Popup p, FlyoutShowMode? flyoutShowMode)
        {
            if (flyoutShowMode.HasValue)
            {
                // This is flyout
                _registeredPopups.Add(new OverlayRegistrationInfo
                {
                    Popup = p,
                    TreatAsLightDismiss = flyoutShowMode == FlyoutShowMode.Standard
                });
            }
            else
            {
                _registeredPopups.Add(new OverlayRegistrationInfo
                {
                    Popup = p,
                    TreatAsLightDismiss = p.IsLightDismissEnabled
                });
            }
            Debug.WriteLine($"Popup registered; total {_registeredPopups.Count}");
        }

        public void UnregisterOverlay(Popup p)
        {
            for (int i = _registeredPopups.Count - 1; i >= 0; i--)
            {
                if (_registeredPopups[i].Popup == p)
                {
                    _registeredPopups.RemoveAt(i);
                    break;
                }
            }
            Debug.WriteLine($"Popup Unregistered; total {_registeredPopups.Count}");
        }

        public IInputElement? GetTopmostLightDismissElement()
        {
            // Windowed popups take priority since they're 'above' the window
            // List is in order the popup was shown, so last item should be
            // most recent popup opened
            if (_registeredPopups.Count > 0)
            {
                var ct = _registeredPopups.Count;
                for (int i = ct - 1; i >= 0; i--)
                {
                    if(_registeredPopups[i].TreatAsLightDismiss)
                    {
                        return _registeredPopups[i].Popup;
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

        private struct OverlayRegistrationInfo
        {
            public Popup Popup;
            public bool TreatAsLightDismiss;
        }
    }
}
