using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Logging;
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

        /// <summary>
        /// Registers a windowed popup into the OverlayLayer. This should be called when the popup opens
        /// </summary>
        /// <remarks>
        /// This is used so that the FocusManager can correctly resolve focus in a search root
        /// that contains open popups. OverlayPopups will be search automatically as part of the
        /// search behavior since they're in the Children collection, but Windowed popups will 
        /// not as they become their own scope separate from the owning window. 
        /// </remarks>
        /// <param name="p">The popup to register</param>
        /// <param name="flyoutShowMode">If the popup is part of a Flyout, the show mode of the 
        /// flyout - which determines how focus is placed into the popup</param>
        public void RegisterOverlay(Popup p, FlyoutShowMode? flyoutShowMode)
        {
            // First check to make sure it's not already registered - that's an error
            for (int i = _registeredPopups.Count - 1; i >= 0; i--)
            {
                if (_registeredPopups[i].Popup == p)
                {
                    throw new InvalidOperationException("Popup is already registered in the OverlayLayer");
                }
            }

            if (flyoutShowMode.HasValue)
            {
                // This is a flyout
                _registeredPopups.Add(new OverlayRegistrationInfo(p, flyoutShowMode == FlyoutShowMode.Standard));
            }
            else
            {
                _registeredPopups.Add(new OverlayRegistrationInfo(p, p.IsLightDismissEnabled));
            }

            Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                .Log(nameof(OverlayLayer), "Popup registered in OverlayLayer, total count {Count}", _registeredPopups.Count);
        }

        /// <summary>
        /// Unregisters a windowed popup from the OverlayLayer. This should be called when the Popup closes
        /// </summary>
        /// <param name="p">The popup to unregister</param>
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

            Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                .Log(nameof(OverlayLayer), "Popup unregistered in OverlayLayer, total count {Count}", _registeredPopups.Count);
        }

        /// <summary>
        /// Retreives the topmost element in the OverlayLayer, searching registered
        /// windowed popups first and then searching the Children collection
        /// </summary>
        /// <returns></returns>
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
                    if (_registeredPopups[i].TreatAsLightDismiss)
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

        private readonly struct OverlayRegistrationInfo
        {
            public OverlayRegistrationInfo(Popup p, bool treatAsLightDismiss)
            {
                Popup = p;
                TreatAsLightDismiss = treatAsLightDismiss;
            }

            public Popup Popup { get; }

            public bool TreatAsLightDismiss { get; }
        }
    }
}
