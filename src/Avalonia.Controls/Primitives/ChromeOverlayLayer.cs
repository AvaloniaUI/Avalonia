using System.Linq;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class ChromeOverlayLayer : Panel, ICustomSimpleHitTest
    {
        public Size AvailableSize { get; private set; }

        public static ChromeOverlayLayer GetOverlayLayer(IVisual visual)
        {
            foreach (var v in visual.GetVisualAncestors())
                if (v is VisualLayerManager vlm)
                    if (vlm.OverlayLayer != null)
                        return vlm.ChromeOverlayLayer;

            if (visual is TopLevel tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.ChromeOverlayLayer;
            }

            return null;
        }

        public bool HitTest(Point point) => Children.HitTestCustom(point);

        protected override Size ArrangeOverride(Size finalSize)
        {
            // We are saving it here since child controls might need to know the entire size of the overlay
            // and Bounds won't be updated in time
            AvailableSize = finalSize;
            return base.ArrangeOverride(finalSize);
        }
    }
}
