using System.Linq;
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
                if (v is VisualLayerManager { TextSelectorLayer: { } textSelectorLayer })
                    return textSelectorLayer;

            if (TopLevel.GetTopLevel(visual) is { } tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.TextSelectorLayer;
            }

            return null;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
                child.Measure(availableSize);
            return default;
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
