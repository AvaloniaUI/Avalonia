using System.Linq;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class TestLayer : Canvas
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public Size AvailableSize { get; private set; }
        public static TestLayer? GetTestLayer(Visual visual)
        {
            foreach(var v in visual.GetVisualAncestors())
                if(v is VisualLayerManager vlm)
                    if (vlm.TestLayer != null)
                        return vlm.TestLayer;
            if (visual is TopLevel tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.TestLayer;
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
