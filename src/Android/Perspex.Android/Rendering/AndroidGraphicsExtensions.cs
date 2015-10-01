using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
using ARect = Android.Graphics.RectF;
using ATextAlign = Android.Graphics.Paint.Align;
using Color = Perspex.Media.Color;
using ATypeFace = Android.Graphics.Typeface;
using ATypeStyle = Android.Graphics.TypefaceStyle;

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
            return new ARect((float) r.X, (float) r.Y, (float) r.Right, (float) r.Bottom);
        }

        public static Rect ToPerspex(this ARect r)
        {
            return new Rect(r.Left, r.Top, r.Width(), r.Height());
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
                    return  ATextAlign.Right;
            }
        }
    }
}