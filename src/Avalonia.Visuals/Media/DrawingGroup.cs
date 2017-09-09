using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media
{
    public class DrawingGroup : AvaloniaObject, IMutableDrawing
    {
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<DrawingGroup, double>(nameof(Opacity), 1);

        public static readonly StyledProperty<Transform> TransformProperty =
            AvaloniaProperty.Register<DrawingGroup, Transform>(nameof(Transform));

        public double Opacity
        {
            get => GetValue(OpacityProperty);
            set => SetValue(OpacityProperty, value);
        }

        public Transform Transform
        {
            get => GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        [Content]
        public AvaloniaList<IDrawing> Children { get; } = new AvaloniaList<IDrawing>();

        double IImage.Width => GetBounds().Width;

        double IImage.Height => GetBounds().Height;

        public void Draw(DrawingContext context)
        {
            Draw(context, Transform?.Value, Opacity, Children);
        }

        private static void Draw(DrawingContext context, Matrix? transform, double opacity, IReadOnlyList<IDrawing> children)
        {
            using (context.PushPreTransform(transform ?? Matrix.Identity))
            using (context.PushOpacity(opacity))
            {
                foreach (var drawing in children)
                {
                    drawing.Draw(context);
                }
            }
        }

        public Rect GetBounds()
        {
            var rect = new Rect();

            foreach (var drawing in Children)
            {
                rect = rect.Union(drawing.GetBounds());
            }

            if (Transform != null)
            {
                rect = rect.TransformToAABB(Transform.Value);
            }

            return rect;
        }

        IDrawing IMutableDrawing.ToImmutable() => 
            new Immutable(Transform?.Value, Opacity, 
                Children.Select(t => t is IMutableDrawing drawing ? drawing.ToImmutable() : t).ToArray(), GetBounds());

        private class Immutable : IDrawing
        {
            private readonly Matrix? _transform;
            private readonly double _opacity;
            private readonly IReadOnlyList<IDrawing> _children;
            private readonly Rect _bounds;

            public Immutable(Matrix? transform, double opacity, IReadOnlyList<IDrawing> children, Rect bounds)
            {
                _transform = transform;
                _opacity = opacity;
                _children = children;
                _bounds = bounds;
            }

            public double Width => _bounds.Width;

            public double Height => _bounds.Height;

            public void Draw(DrawingContext context) => DrawingGroup.Draw(context, _transform, _opacity, _children);

            public Rect GetBounds() => _bounds;
        }
    }
}