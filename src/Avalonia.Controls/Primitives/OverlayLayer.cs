using System.Linq;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayLayer : Canvas
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public Size AvailableSize { get; private set; }
        public static OverlayLayer? GetOverlayLayer(Visual visual)
        {
            foreach(var v in visual.GetVisualAncestors())
                if(v is VisualLayerManager vlm)
                    if (vlm.OverlayLayer != null)
                        return vlm.OverlayLayer;
            if (TopLevel.GetTopLevel(visual) is {} tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.OverlayLayer;
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
    }
}
