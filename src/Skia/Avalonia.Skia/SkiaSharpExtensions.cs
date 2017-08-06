using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;


namespace Avalonia
{
    public static class SkiaSharpExtensions
    {
        public static SKPoint ToSKPoint(this Point p)
        {
            return new SKPoint((float)p.X, (float)p.Y);
        }

        public static SKRect ToSKRect(this Rect r)
        {
            return new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);
        }

        public static Rect ToAvaloniaRect(this SKRect r)
        {
            return new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }

        public static SKMatrix ToSKMatrix(this Matrix m)
        {
            var sm = new SKMatrix
            {
                ScaleX = (float)m.M11,
                SkewX = (float)m.M21,
                TransX = (float)m.M31,
                SkewY = (float)m.M12,
                ScaleY = (float)m.M22,
                TransY = (float)m.M32,
                Persp0 = 0,
                Persp1 = 0,
                Persp2 = 1
            };

            return sm;
        }

        public static SKColor ToSKColor(this Media.Color c)
        {
            return new SKColor(c.R, c.G, c.B, c.A);
        }

        public static SKColorType ToSkColorType(this PixelFormat fmt)
        {
            if (fmt == PixelFormat.Rgb565)
                return SKColorType.Rgb565;
            if (fmt == PixelFormat.Bgra8888)
                return SKColorType.Bgra8888;
            if (fmt == PixelFormat.Rgba8888)
                return SKColorType.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public static PixelFormat ToPixelFormat(this SKColorType fmt)
        {
            if (fmt == SKColorType.Rgb565)
                return PixelFormat.Rgb565;
            if (fmt == SKColorType.Bgra8888)
                return PixelFormat.Bgra8888;
            if (fmt == SKColorType.Rgba8888)
                return PixelFormat.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public static SKShaderTileMode ToSKShaderTileMode(this Media.GradientSpreadMethod m)
        {
            switch (m)
            {
                default:
                case Media.GradientSpreadMethod.Pad: return SKShaderTileMode.Clamp;
                case Media.GradientSpreadMethod.Reflect: return SKShaderTileMode.Mirror;
                case Media.GradientSpreadMethod.Repeat: return SKShaderTileMode.Repeat;
            }
        }

        public static SKTextAlign ToSKTextAlign(this TextAlignment a)
        {
            switch (a)
            {
                default:
                case TextAlignment.Left: return SKTextAlign.Left;
                case TextAlignment.Center: return SKTextAlign.Center;
                case TextAlignment.Right: return SKTextAlign.Right;
            }
        }

        public static TextAlignment ToAvalonia(this SKTextAlign a)
        {
            switch (a)
            {
                default:
                case SKTextAlign.Left: return TextAlignment.Left;
                case SKTextAlign.Center: return TextAlignment.Center;
                case SKTextAlign.Right: return TextAlignment.Right;
            }
        }

        public static SKPath Clone(this SKPath src)
        {
            return src != null ? new SKPath(src) : null;
        }
    }
}
