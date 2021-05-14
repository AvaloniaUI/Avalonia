using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class ManagedPointer : Control
    {
        private Point _p = new Point();
        private readonly TopLevel _topLevel;
        
        public ManagedPointer(TopLevel visualRoot)
        {
            _topLevel = visualRoot;
            visualRoot.PointerMoved += VisualRootOnPointerMoved;
            
            var layer = AdornerLayer.GetAdornerLayer(visualRoot);

            AdornerLayer.SetAdornedElement(this, visualRoot);
            ((ISetLogicalParent)this).SetParent(visualRoot);
            layer.Children.Add(this);
            
        }

        private void VisualRootOnPointerMoved(object sender, PointerEventArgs e)
        {
            _p = e.GetPosition(_topLevel);
            
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.FillRectangle(Brushes.Black, new Rect(_p, new Size(10, 10)));
        }
    }
}
