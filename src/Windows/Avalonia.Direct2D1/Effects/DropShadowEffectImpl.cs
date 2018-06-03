using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Visuals.Effects;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct2D1.Effects;
using SharpDX.Mathematics.Interop;
using Color = Avalonia.Media.Color;
using Effect = SharpDX.Direct2D1.Effect;

namespace Avalonia.Direct2D1.Effects
{
    class DropShadowEffectImpl: IDropShadowEffectImpl, IDirect2DPlatformEffectImpl
    {
        private float _offsetX;
        private float _offsetY;
        private float _blur;
        private Color _color;

        public double OffsetX
        {
            get => _offsetX;
            set => _offsetX = (float)value;
        }

        public double OffsetY
        {
            get => _offsetY;
            set => _offsetX = (float)value;
        }

        public double Blur
        {
            get => _blur;
            set => _blur = (float)value;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public DropShadowEffectImpl(double offsetX, double offsetY, double blur, Color color)
        {
            _offsetX = (float)offsetX;
            _offsetY = (float)offsetY;
            _blur = (float)blur;
            _color = color;
        }

        public void Render(DeviceContext context, Bitmap bitmap)
        {
            var shadowEffect = new Shadow(context);
            var affineTransformEffect = new AffineTransform2D(context);

            shadowEffect.Color = _color.ToDirect2D();
            shadowEffect.BlurStandardDeviation = _blur;

            affineTransformEffect.TransformMatrix = Matrix3x2.Translation(_offsetX, _offsetY);

            shadowEffect.SetInput(0, bitmap, false);

            affineTransformEffect.SetInputEffect(0, shadowEffect);

            context.DrawImage(affineTransformEffect);
            context.DrawBitmap(bitmap, 1.0f, BitmapInterpolationMode.Linear);
        }
    }
}
