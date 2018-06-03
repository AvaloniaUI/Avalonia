using Avalonia.Media;
using Avalonia.Visuals.Effects;
using SkiaSharp;

namespace Avalonia.Skia.Effects
{
    internal class DropShadowEffectImpl : IDropShadowEffectImpl, ISkiaPlatformEffectImpl
    {
        private float _offsetX;
        private float _offsetY;
        private float _blur;
        private Color _color;

        public double OffsetX
        {
            get => _offsetX;
            set => _offsetX = (float) value;
        }

        public double OffsetY
        {
            get => _offsetY;
            set => _offsetX = (float) value;
        }

        public double Blur
        {
            get => _blur;
            set => _blur = (float) value;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public DropShadowEffectImpl(double offsetX, double offsetY, double blur, Color color)
        {
            _offsetX = (float) offsetX;
            _offsetY = (float) offsetY;
            _blur = (float) blur;
            _color = color;
        }

        public void Render(SKPaint paint)
        {
            using (var filter = SKImageFilter.CreateDropShadow(_offsetX, _offsetY, _blur, _blur, _color.ToSKColor(),
                SKDropShadowImageFilterShadowMode.DrawShadowAndForeground, null))
            {
                paint.IsAntialias = true;
                paint.ImageFilter = filter;
            }
        }
    }
}