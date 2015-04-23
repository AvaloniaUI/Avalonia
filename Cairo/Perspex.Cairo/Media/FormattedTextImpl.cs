// -----------------------------------------------------------------------
// <copyright file="FormattedTextImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;

    public class FormattedTextImpl : IFormattedTextImpl
    {
        public FormattedTextImpl(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            var context = Locator.Current.GetService<Pango.Context>();
            this.Layout = new Pango.Layout(context);
            this.Layout.SetText(text);
            this.Layout.FontDescription = new Pango.FontDescription
            {
                Family = fontFamily,
                Size = Pango.Units.FromDouble(fontSize),
                Style = (Pango.Style)fontStyle,
                Weight = fontWeight.ToCairo()
            };

            this.Layout.Alignment = textAlignment.ToCairo();
        }

        public Size Constraint
        {
            get
            {
                return new Size(this.Layout.Width, double.PositiveInfinity);
            }

            set
            {

                this.Layout.Width = Pango.Units.FromDouble(value.Width);
            }
        }

        public Pango.Layout Layout
        {
            get;
            private set;
        }

        public void Dispose()
        {
            this.Layout.Dispose();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return new FormattedTextLine[0];
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            int textPosition;
            int trailing;

            var isInside = this.Layout.XyToIndex(
                Pango.Units.FromDouble(point.X),
                Pango.Units.FromDouble(point.Y),
                out textPosition,
                out trailing);

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = textPosition,
                IsTrailing = trailing == 0,
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            return this.Layout.IndexToPos(index).ToPerspex();
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length, Point origin)
        {
            // TODO: Implement.
            return new Rect[0];
        }

        public Size Measure()
        {
            int width;
            int height;
            this.Layout.GetPixelSize(out width, out height);
        
            if (this.Layout.Alignment == Pango.Alignment.Right)
            {
                return new Size(width, height);
            }

            return new Size(width, height);
        }

        public void SetForegroundBrush(Brush brush, int startIndex, int count)
        {
            // TODO: Implement.
        }
    }
}
