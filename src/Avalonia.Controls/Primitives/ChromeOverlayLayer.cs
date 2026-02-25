using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class ChromeOverlayLayer : Panel
    {
        public static ChromeOverlayLayer? GetChromeOverlayLayer(Visual visual)
        {
            foreach (var v in visual.GetVisualAncestors())
                if (v is VisualLayerManager { ChromeOverlayLayer: { } layer })
                    return layer;

            if (TopLevel.GetTopLevel(visual) is { } tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.ChromeOverlayLayer;
            }

            return null;
        }

        public void Add(Control control)
        {
            Children.Add(control);
        }
    }
}
