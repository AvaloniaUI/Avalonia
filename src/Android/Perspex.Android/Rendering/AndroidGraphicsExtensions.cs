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

        public static AMatrix ToAndroidGraphics(this Matrix matrix)
        {
			var transformValues = new float[9];
			transformValues[0] = (float)matrix.M11;
			transformValues[1] = (float)matrix.M21;
			transformValues[2] = (float)matrix.M31;

			transformValues[3] = (float)matrix.M12;
			transformValues[4] = (float)matrix.M22;
			transformValues[5] = (float)matrix.M32;

			transformValues[6] = 0;
			transformValues[7] = 0;
			transformValues[8] = 1;
			var am = new AMatrix();
			am.SetValues(transformValues);
            return am;
        }

        public static Matrix ToPerspex(this AMatrix m)
        {
            float[] v = new float[9];
            m.GetValues(v);
			return new Matrix(v[0],v[3], v[1], v[4], v[2], v[5]);
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