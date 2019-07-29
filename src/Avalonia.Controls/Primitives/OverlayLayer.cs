using System.Linq;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayLayer : Canvas, ICustomSimpleHitTest
    {
        public Size AvailableSize { get; private set; }
        public static OverlayLayer GetOverlayLayer(IVisual visual)
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
        
        public bool HitTest(Point point)
        {
            return Children.Any(ctrl => ctrl.TransformedBounds?.Contains(point) == true);
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
