using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a surface for showing overlays.
    /// Overlays are displayed on top of other elements, but behind popups.
    /// </summary>
    public class OverlayLayer : Canvas
    {
        protected override bool BypassFlowDirectionPolicies => true;

        public Size AvailableSize { get; private set; }

        /// <summary>
        /// Gets the dedicated adorner layer for this overlay layer.
        /// </summary>
        internal AdornerLayer? AdornerLayer { get; set; }

        internal OverlayLayer()
        {
        }

        /// <summary>
        /// Retrieves the overlay layer associated with the specified visual, if any.
        /// </summary>
        /// <param name="visual">The visual for which to retrieve the associated overlay layer.</param>
        /// <returns>The <see cref="OverlayLayer"/> associated with the visual, or null if no overlay layer exists.</returns>
        public static OverlayLayer? GetOverlayLayer(Visual visual)
        {
            foreach (var v in visual.GetVisualAncestors())
                if (v is VisualLayerManager { OverlayLayer: { } layer })
                    return layer;

            if (TopLevel.GetTopLevel(visual) is { } tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.OverlayLayer;
            }

            return null;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
                child.Measure(availableSize);
            return availableSize;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            // We are saving it here since child controls might need to know the entire size of the overlay
            // and Bounds won't be updated in time
            AvailableSize = finalSize;
            return base.ArrangeOverride(finalSize);
        }
    }
}
