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
                Size = Pango.Units.FromDouble(fontSize * 0.73),
                Style = (Pango.Style)fontStyle,
                Weight = fontWeight.ToCairo()
            };
            
            this.Layout.Alignment = textAlignment.ToCairo();
        }

        private Size size;
        public Size Constraint
        {
            get
            {
                return size;
            }

            set
            {
                this.size = value;
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

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var ranges = new List<Rect>();
        
            for (var i = 0; i < length; i++)
            {
                ranges.Add(this.HitTestTextPosition(index+i));
            }
            
            return ranges;
        }

        public Size Measure()
        {
            int width;
            int height;
            this.Layout.GetPixelSize(out width, out height);
        
            return new Size(width, height);
        }

        public void SetForegroundBrush(Brush brush, int startIndex, int count)
        {
            // TODO: Implement.
        }
    }
}
