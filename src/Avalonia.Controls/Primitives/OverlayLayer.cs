using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayLayer : Control
    {
        /// <summary>
        /// Defines the Left attached property.
        /// </summary>
        public static readonly AttachedProperty<double> LeftProperty =
            AvaloniaProperty.RegisterAttached<OverlayLayer, Control, double>("Left", 0);

        /// <summary>
        /// Defines the Top attached property.
        /// </summary>
        public static readonly AttachedProperty<double> TopProperty =
            AvaloniaProperty.RegisterAttached<OverlayLayer, Control, double>("Top", 0);

        /// <summary>
        /// Defines the InfiniteAvailableSize attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> InfiniteAvailableSizeProperty =
            AvaloniaProperty.RegisterAttached<OverlayLayer, Control, bool>("InfiniteAvailableSize", false);


        static OverlayLayer()
        {
            foreach (var p in new []{LeftProperty, TopProperty})
            {
                p.Changed.AddClassHandler<Control>((target, e) =>
                {
                    if (target.GetVisualParent() is OverlayLayer layer)
                        layer.InvalidateArrange();
                });
            }
        }
        
        public Size AvailableSize { get; private set; }
        
        /// <summary>
        /// Gets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's left coordinate.</returns>
        public static double GetLeft(AvaloniaObject element)
        {
            return element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Sets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetLeft(AvaloniaObject element, double value)
        {
            element.SetValue(LeftProperty, value);
        }
        
        /// <summary>
        /// Gets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's top coordinate.</returns>
        public static double GetTop(AvaloniaObject element)
        {
            return element.GetValue(TopProperty);
        }

        /// <summary>
        /// Sets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The top value.</param>
        public static void SetTop(AvaloniaObject element, double value)
        {
            element.SetValue(TopProperty, value);
        }
        
        /// <summary>
        /// Gets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's top coordinate.</returns>
        public static bool GetInfiniteAvailableSize(AvaloniaObject element)
        {
            return element.GetValue(InfiniteAvailableSizeProperty);
        }

        /// <summary>
        /// Sets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The top value.</param>
        public static void SetInfiniteAvailableSize(AvaloniaObject element, bool value)
        {
            element.SetValue(InfiniteAvailableSizeProperty, value);
        }

        
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

        public void Add(Control v)
        {
            VisualChildren.Add(v);
            InvalidateMeasure();
            InvalidateArrange();
        }

        public void Remove(Control v) => VisualChildren.Remove(v);
        
        protected override Size MeasureOverride(Size availableSize)
        {
            
            var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (Control v in VisualChildren)
                v.Measure(GetInfiniteAvailableSize(v) ? infinite : availableSize);

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // We are saving it here since child controls might need to know the entire size of the overlay
            // and Bounds won't be updated in time
            AvailableSize = finalSize;
            foreach (Control v in VisualChildren)
                v.Arrange(new Rect(GetLeft(v), GetTop(v), v.DesiredSize.Width, v.DesiredSize.Height));
            return finalSize;
        }
    }
}
