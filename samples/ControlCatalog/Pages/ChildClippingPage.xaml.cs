using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ChildClippingPage : UserControl
    {
        public ChildClippingPage()
        {
            InitializeComponent();
        }
    }

    public sealed class StarLayoutClipDecorator : Decorator
    {
        private Geometry? _clipGeometry;
        private Size _clipGeometrySize;

        protected override Geometry? GetLayoutClip(Size layoutSlotSize)
        {
            return GetClipGeometry(Bounds.Size);
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));
            base.Render(context);
        }

        private Geometry GetClipGeometry(Size size)
        {
            if (_clipGeometry != null && _clipGeometrySize == size)
                return _clipGeometry;

            _clipGeometrySize = size;
            _clipGeometry = BuildStarGeometry(size);
            return _clipGeometry;
        }

        private static Geometry BuildStarGeometry(Size size)
        {
            var geometry = new StreamGeometry();
            var center = new Point(size.Width * 0.5, size.Height * 0.5);
            var outerRadius = Math.Min(size.Width, size.Height) * 0.45;
            var innerRadius = outerRadius * 0.45;

            using (var ctx = geometry.Open())
            {
                var angleStep = Math.PI / 5.0;
                for (var i = 0; i < 10; i++)
                {
                    var radius = (i % 2 == 0) ? outerRadius : innerRadius;
                    var angle = -Math.PI * 0.5 + angleStep * i;
                    var point = new Point(
                        center.X + Math.Cos(angle) * radius,
                        center.Y + Math.Sin(angle) * radius);

                    if (i == 0)
                        ctx.BeginFigure(point, true);
                    else
                        ctx.LineTo(point);
                }

                ctx.EndFigure(true);
            }

            return geometry;
        }
    }

}
