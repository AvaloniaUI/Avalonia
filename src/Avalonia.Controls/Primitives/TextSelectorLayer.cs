using System.Linq;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class TextSelectorLayer : Canvas
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public Size AvailableSize { get; private set; }
        public static TextSelectorLayer? GetTextSelectorLayer(Visual visual)
        {
            foreach (var v in visual.GetVisualAncestors())
                if (v is VisualLayerManager vlm)
                    if (vlm.TextSelectorLayer != null)
                        return vlm.TextSelectorLayer;
            if (visual is TopLevel tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.TextSelectorLayer;
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
            AvailableSize = finalSize;
            return base.ArrangeOverride(finalSize);
        }

        public void Add(Control control)
        {
            Children.Add(control);
        }

        public void Remove(Control control)
        {
            Children.Remove(control);
        }
    }
}
