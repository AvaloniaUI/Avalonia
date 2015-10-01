using CoreGraphics;
using CoreText;
using Perspex.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace Perspex.iOS.Rendering
{
    public static class CoreGraphicsExtensions
    {
        public static CGColor ToCoreGraphics(this Perspex.Media.Color color)
        {
            return new CGColor((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f, (float)color.A / 255.0f);
        }

        public static CGAffineTransform ToCoreGraphics(this Matrix m)
        {
            return new CGAffineTransform((float)m.M11, (float)m.M12, (float)m.M21, (float)m.M22, (float)m.M31, (float)m.M32);
        }

        public static Matrix ToPerspex(this CGAffineTransform m)
        {
            return new Matrix(m.xx, m.yx, m.xy, m.yy, m.x0, m.y0);
        }

        public static CGPoint ToCoreGraphics(this Point p)
        {
            return new CGPoint(p.X, p.Y);
        }

        public static Point ToPerspex(this CGPoint pt)
        {
            return new Point(pt.X, pt.Y);
        }

        public static CGRect ToCoreGraphics(this Rect rect)
        {
            return new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Rect ToPerspex(this CGRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static CGSize ToCoreGraphics(this Size rect)
        {
            return new CGSize(rect.Width, rect.Height);
        }

        public static Size ToPerspex(this CGSize size)
        {
            return new Size(size.Width, size.Height);
        }

        public static CGLineJoin ToCoreGraphics(this PenLineJoin join)
        {
            switch (join)
            {
                default:
                case PenLineJoin.Miter:
                    return CGLineJoin.Miter;
                case PenLineJoin.Round:
                    return CGLineJoin.Round;
                case PenLineJoin.Bevel:
                    return CGLineJoin.Bevel;
            }
        }

        public static CGLineCap ToCoreGraphics(this PenLineCap cap)
        {
            switch (cap)
            {
                default:
                case PenLineCap.Flat:
                    return CGLineCap.Butt;
                case PenLineCap.Round:
                    return CGLineCap.Round;
                case PenLineCap.Square:
                    return CGLineCap.Square;
                case PenLineCap.Triangle:
                    return CGLineCap.Butt;  // no platform support for this!!
            }
        }

        public static CTTextAlignment ToCoreGraphics(this TextAlignment align)
        {
            switch (align)
            {
                default:
                case TextAlignment.Left:
                    return CTTextAlignment.Left;
                case TextAlignment.Center:
                    return CTTextAlignment.Center;
                case TextAlignment.Right:
                    return CTTextAlignment.Right;
            }
        }
    }
}
