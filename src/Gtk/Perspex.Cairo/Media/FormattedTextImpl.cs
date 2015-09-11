// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Media;
using Perspex.Platform;
using Splat;

namespace Perspex.Cairo.Media
{
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
            Layout = new Pango.Layout(context);
            Layout.SetText(text);
            Layout.FontDescription = new Pango.FontDescription
            {
                Family = fontFamily,
                Size = Pango.Units.FromDouble(fontSize),
                Style = (Pango.Style)fontStyle,
                Weight = fontWeight.ToCairo()
            };

            Layout.Attributes = new Pango.AttrList();
            Layout.Alignment = textAlignment.ToCairo();
        }

        private Size _size;
        public Size Constraint
        {
            get
            {
                return _size;
            }

            set
            {
                _size = value;
                Layout.Width = Pango.Units.FromDouble(value.Width);
            }
        }

        public Pango.Layout Layout
        {
            get; }

        public void Dispose()
        {
            Layout.Dispose();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return new FormattedTextLine[0];
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            int textPosition;
            int trailing;

            var isInside = Layout.XyToIndex(
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
            return Layout.IndexToPos(index).ToPerspex();
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var ranges = new List<Rect>();

            for (var i = 0; i < length; i++)
            {
                ranges.Add(HitTestTextPosition(index + i));
            }

            return ranges;
        }

        public Size Measure()
        {
            int width;
            int height;
            Layout.GetPixelSize(out width, out height);

            return new Size(width, height);
        }

        public void SetFormatting(Brush brush, FontWeight weight, double fontSize, int startIndex, int count)
        {
            var scb = brush as SolidColorBrush;
            var weightAttr = new Pango.AttrWeight(weight.ToCairo());
            var fontSizeAttr = new Pango.AttrSize(Pango.Units.FromDouble(fontSize));

            weightAttr.StartIndex = (uint)startIndex;
            weightAttr.EndIndex = (uint)(startIndex + count);

            fontSizeAttr.StartIndex = (uint)startIndex;
            fontSizeAttr.EndIndex = (uint)(startIndex + count);

            if (scb != null)
            {

                var color = new Pango.Color();
                color.Parse(string.Format("#{0}", scb.Color.ToString().Substring(3)));

                var brushAttr = new Pango.AttrForeground(color);
                brushAttr.StartIndex = (uint)startIndex;
                brushAttr.EndIndex = (uint)(startIndex + count);

                Layout.Attributes.Insert(brushAttr);
            }

            Layout.Attributes.Insert(weightAttr);
            Layout.Attributes.Insert(fontSizeAttr);
        }
    }
}
