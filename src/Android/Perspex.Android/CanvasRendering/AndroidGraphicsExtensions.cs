using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Specific;
using Perspex.Media;
using System;
using AAllignment = Android.Text.Layout.Alignment;
using ACap = Android.Graphics.Paint.Cap;
using AColor = Android.Graphics.Color;
using AJoin = Android.Graphics.Paint.Join;
using AMatrix = Android.Graphics.Matrix;
using APoint = Android.Graphics.PointF;
using ARect = Android.Graphics.Rect;
using ARectF = Android.Graphics.RectF;
using AStyle = Android.Graphics.Paint.Style;
using ATextAlign = Android.Graphics.Paint.Align;
using ATileMode = Android.Graphics.Shader.TileMode;
using ATypeface = Android.Graphics.Typeface;
using ATypeStyle = Android.Graphics.TypefaceStyle;

namespace Perspex.Android.CanvasRendering
{
    public static class AndroidGraphicsExtensions
    {
        public static AColor ToAndroidGraphics(this Color c)
        {
            return new AColor(c.R, c.G, c.B, c.A);
        }

        public static AMatrix ToAndroidGraphics(this Matrix matrix)
        {
            var ps = PointUnitService.Instance;
            //TODO: !!!
            var transformValues = new float[9];
            transformValues[0] = (float)matrix.M11;
            transformValues[1] = (float)matrix.M21;
            transformValues[2] = ps.PerspexToNativeXF(matrix.M31);
            //transformValues[2] = (float)matrix.M31;

            transformValues[3] = (float)matrix.M12;
            transformValues[4] = (float)matrix.M22;
            transformValues[5] = ps.PerspexToNativeYF(matrix.M32);
            //transformValues[5] = (float)matrix.M32;

            transformValues[6] = 0;
            transformValues[7] = 0;
            transformValues[8] = 1;
            var am = new AMatrix();
            am.SetValues(transformValues);
            return am;
        }

        //public static Matrix ToPerspex(this AMatrix m)
        //{
        //    var v = new float[9];
        //    m.GetValues(v);
        //    return new Matrix(v[0], v[3], v[1], v[4], v[2], v[5]);
        //}

        public static APoint ToAndroidGraphics(this Point p, bool notransform = false)
        {
            if (notransform) new APoint((float)p.X, (float)p.Y);
            var ps = PointUnitService.Instance;
            return new APoint(ps.PerspexToNativeXF(p.X), ps.PerspexToNativeYF(p.Y));
        }

        //public static Point ToPerspex(this APoint p)
        //{
        //    return new Point(p.X, p.Y);
        //}

        public static ARect ToAndroidGraphics(this Rect r, bool notransform = false)
        {
            if (notransform) return new ARect((int)r.X, (int)r.Y, (int)r.Right, (int)r.Bottom);
            var ps = PointUnitService.Instance;
            return new ARect(ps.PerspexToNativeXInt(r.X), ps.PerspexToNativeYInt(r.Y), ps.PerspexToNativeXInt(r.Right), ps.PerspexToNativeYInt(r.Bottom));
        }

        public static ARectF ToAndroidGraphicsF(this Rect r)
        {
            var ps = PointUnitService.Instance;
            return new ARectF(ps.PerspexToNativeXF(r.X), ps.PerspexToNativeYF(r.Y), ps.PerspexToNativeXF(r.Right), ps.PerspexToNativeYF(r.Bottom));
        }

        //public static Rect ToPerspex(this ARect r)
        //{
        //    return new Rect(r.Left, r.Top, r.Width(), r.Height());
        //}

        public static Rect ToPerspex(this ARectF r)
        {
            var ps = PointUnitService.Instance;
            return new Rect(ps.NativeToPerspexX(r.Left), ps.NativeToPerspexY(r.Top), ps.NativeToPerspexX(r.Width()), ps.NativeToPerspexY(r.Height()));
        }

        public static AJoin ToAndroidGraphics(this PenLineJoin plj)
        {
            switch (plj)
            {
                default:
                case PenLineJoin.Bevel:
                    return AJoin.Bevel;

                case PenLineJoin.Miter:
                    return AJoin.Miter;

                case PenLineJoin.Round:
                    return AJoin.Round;
            }
        }

        public static ACap ToAndroidGraphics(this PenLineCap plc)
        {
            switch (plc)
            {
                default:
                case PenLineCap.Flat:
                    return ACap.Butt;

                case PenLineCap.Round:
                    return ACap.Round;

                case PenLineCap.Square:
                    return ACap.Square;
                //Triangle not supported
                case PenLineCap.Triangle:
                    goto default;
            }
        }

        public static AStyle ToAndroidGraphics(this BrushUsage bu)
        {
            switch (bu)
            {
                default:
                case BrushUsage.Fill:
                    return AStyle.Fill;

                case BrushUsage.Stroke:
                    return AStyle.Stroke;

                case BrushUsage.Both:
                    return AStyle.FillAndStroke;
            }
        }

        public static ATypeface ToAndroidGraphicsTypeface(this FontStyle s, FontWeight fontWeight)
        {
            ATypeface result = ATypeface.Default;

            if (fontWeight >= FontWeight.Bold)
            {
                result = ATypeface.DefaultBold;
            }

            return result;
        }

        public static ATypeStyle ToAndroidGraphicsTypefaceStyle(this FontStyle s, FontWeight fontWeight)
        {
            ATypeStyle result;
            switch (s)
            {
                default:
                case FontStyle.Normal:
                    result = ATypeStyle.Normal;
                    break;

                case FontStyle.Italic:
                    result = ATypeStyle.Italic;
                    break;
                //Oblique not supported, return normal
                case FontStyle.Oblique:
                    goto default;
            }

            if (fontWeight >= FontWeight.Bold)
            {
                result = result == ATypeStyle.Italic ? ATypeStyle.BoldItalic : ATypeStyle.Bold;
            }

            return result;
        }

        public static ATextAlign ToAndroidGraphics(this TextAlignment ta)
        {
            switch (ta)
            {
                default:
                case TextAlignment.Left:
                    return ATextAlign.Left;

                case TextAlignment.Center:
                    return ATextAlign.Center;

                case TextAlignment.Right:
                    return ATextAlign.Right;
            }
        }

        public static AAllignment ToAndroidGraphicsLayoutAligment(this TextAlignment ta)
        {
            switch (ta)
            {
                default:
                case TextAlignment.Left:
                    return AAllignment.AlignNormal;

                case TextAlignment.Center:
                    return AAllignment.AlignCenter;

                case TextAlignment.Right:
                    return AAllignment.AlignOpposite;
            }
        }

        public static ATileMode ToAndroidGraphicsX(this TileMode tileMode)
        {
            switch (tileMode)
            {
                case TileMode.Tile: return ATileMode.Repeat;
                case TileMode.FlipX: return ATileMode.Mirror;
                case TileMode.FlipXY: return ATileMode.Mirror;
                case TileMode.FlipY: return ATileMode.Clamp;
                case TileMode.None: return ATileMode.Clamp;
                default: return ATileMode.Clamp;
            }
        }

        public static ATileMode ToAndroidGraphicsY(this TileMode tileMode)
        {
            switch (tileMode)
            {
                case TileMode.Tile: return ATileMode.Repeat;
                case TileMode.FlipX: return ATileMode.Clamp;
                case TileMode.FlipXY: return ATileMode.Mirror;
                case TileMode.FlipY: return ATileMode.Mirror;
                case TileMode.None: return ATileMode.Clamp;
                default: return ATileMode.Clamp;
            }
        }

        public static int OpacityToAndroidAlfa(double opacity)
        {
            return Convert.ToInt32(opacity * 255);
        }
    }
}