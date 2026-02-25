using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class PopupOverlayLayer : Canvas
    {
        protected override bool BypassFlowDirectionPolicies => true;

        public Size AvailableSize { get; private set; }

        public static PopupOverlayLayer? GetPopupOverlayLayer(Visual visual)
        {
            foreach (var v in visual.GetVisualAncestors())
                if (v is VisualLayerManager { PopupOverlayLayer: { } layer })
                    return layer;

            if (TopLevel.GetTopLevel(visual) is { } tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.PopupOverlayLayer;
            }

            return null;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
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
    }
}
