using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Java.Nio.Channels;
using Perspex.Media;
using AColor = Android.Graphics.Color;
using AMatrix = Android.Graphics.Matrix;
using APoint = Android.Graphics.PointF;
using ARectF = Android.Graphics.RectF;
using ARect = Android.Graphics.Rect;
using ATextAlign = Android.Graphics.Paint.Align;
using Color = Perspex.Media.Color;
using ATypeFace = Android.Graphics.Typeface;
using ATypeStyle = Android.Graphics.TypefaceStyle;
using AStyle = Android.Graphics.Paint.Style;
using AJoin = Android.Graphics.Paint.Join;
using ACap = Android.Graphics.Paint.Cap;

namespace Perspex.Android.Rendering
{
    public static class AndroidGraphicsExtensions
    {
        public static AColor ToAndroidGraphics(this Color c)
        {
            return new AColor(c.R, c.G, c.B, c.A);
        }

        public static AMatrix ToAndroidGraphics(this Matrix m)
        {
            AMatrix am = new AMatrix();
            am.SetValues(new[] {(float)m.M11, (float)m.M12, 0,
                (float)m.M21, (float)m.M22, 0,
                (float)m.M31, (float)m.M32, 1});
            return am;
        }

        public static Matrix ToPerspex(this AMatrix m)
        {
            float[] v = new float[9];
            m.GetValues(v);
            return new Matrix(v[0], v[1], v[3], v[4], v[6], v[7]);
        }

        public static APoint ToAndroidGraphics(this Point p)
        {
            return new APoint((float)p.X, (float)p.Y);
        }

        public static Point ToPerspex(this APoint p)
        {
            return new Point(p.X, p.Y);
        }

        public static ARect ToAndroidGraphics(this Rect r)
        {
            return new ARect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
        }

        public static Rect ToPerspex(this ARect r)
        {
            return new Rect(r.Left, r.Top, r.Width(), r.Height());
        }

        public static ARectF ToAndroidGraphicsF(this Rect r)
        {
            //return new ARectF((float) r.Width, (float) r.Height, (float) r.X, (float) r.Y);
            return new ARectF((float) r.X, (float) r.Y, (float) r.Width, (float) r.Height);
        }

        public static Rect ToPerspex(this ARectF r)
        {
            return new Rect(r.Left, r.Top, r.Width(), r.Height());
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

        public static ATypeStyle ToAndroidGraphics(this FontStyle s)
        {
            switch (s)
            {
                default:
                case FontStyle.Normal:
                    return ATypeStyle.Normal;
                case FontStyle.Italic:
                    return ATypeStyle.Italic;
                //Oblique not supported, return normal
                case FontStyle.Oblique:
                    goto default;
            }
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
    }
}