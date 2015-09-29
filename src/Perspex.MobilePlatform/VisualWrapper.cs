using Perspex.Collections;
using Perspex.Media;

namespace Perspex.MobilePlatform
{
    class VisualWrapper : IVisual
    {
        private readonly MobileTopLevel _topLevel;
        private readonly PerspexList<IVisual> _visualChildren = new PerspexList<IVisual>();
        public VisualWrapper(MobileTopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        public Rect Bounds => new Rect((_topLevel.Renderer.CapturedVisual?.Bounds ?? default(Rect)).Size);
        public bool ClipToBounds { get; set; } = true;
        public bool IsAttachedToVisualTree => true;
        public bool IsEffectivelyVisible => true;
        public bool IsVisible { get; set; } = true;
        public double Opacity { get; set; } = 1;
        public Transform RenderTransform { get; set; }
        public RelativePoint TransformOrigin { get; set; }
        public IPerspexReadOnlyList<IVisual> VisualChildren => _visualChildren;
        public IVisual VisualParent => Platform.Scene;
        public int ZIndex { get; set; }
        public void Render(IDrawingContext context)
        {
            _visualChildren.Clear();
            if (_topLevel.Renderer.CapturedVisual != null)
                _visualChildren.Add(_topLevel.Renderer.CapturedVisual);
        }

        public Matrix TransformToVisual(IVisual visual)
        {
            return Matrix.Identity;
        }
    }
}
