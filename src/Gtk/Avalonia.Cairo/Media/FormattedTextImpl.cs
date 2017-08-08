// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Cairo.Media
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        private Size _constraint;

        static double CorrectScale(double input)
        {
            return input * 0.75;
        }

        public FormattedTextImpl(
            Pango.Context context,
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            Contract.Requires<ArgumentNullException>(context != null);
            Contract.Requires<ArgumentNullException>(text != null);

            Layout = new Pango.Layout(context);
            Layout.SetText(text);

            Layout.FontDescription = new Pango.FontDescription
            {
                Family = typeface?.FontFamilyName ?? "monospace",
                Size = Pango.Units.FromDouble(CorrectScale(typeface?.FontSize ?? 12)),
                Style = (Pango.Style)(typeface?.Style ?? FontStyle.Normal),
                Weight = (typeface?.Weight ?? FontWeight.Normal).ToCairo(),
            };

            Layout.Alignment = textAlignment.ToCairo();
            Layout.Attributes = new Pango.AttrList();
            Layout.Width = double.IsPositiveInfinity(constraint.Width) ? -1 : Pango.Units.FromDouble(constraint.Width);

            if (spans != null)
            {
                foreach (var span in spans)
                {
                    if (span.ForegroundBrush is SolidColorBrush scb)
                    {
                        var color = new Pango.Color();
                        color.Parse(string.Format("#{0}", scb.Color.ToString().Substring(3)));

                        var brushAttr = new Pango.AttrForeground(color);
                        brushAttr.StartIndex = (uint)TextIndexToPangoIndex(span.StartIndex);
                        brushAttr.EndIndex = (uint)TextIndexToPangoIndex(span.StartIndex + span.Length);

                        this.Layout.Attributes.Insert(brushAttr);
                    }
                }
            }

            Size = Measure();
        }

        public FormattedTextImpl(Pango.Layout layout)
        {
            Layout = layout;
            Size = Measure();
        }

        public string Text => Layout.Text;

        public Size Constraint => _constraint;

        public Size Size { get; }

        public Pango.Layout Layout { get; }

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

            textPosition = PangoIndexToTextIndex(textPosition);

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = textPosition,
                IsTrailing = trailing == 0,
            };
        }

        int PangoIndexToTextIndex(int pangoIndex)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(Text), 0, Math.Min(pangoIndex, Text.Length)).Length;
        }

        public Rect HitTestTextPosition(int index)
        {
            return Layout.IndexToPos(TextIndexToPangoIndex(index)).ToAvalonia();
        }

        int TextIndexToPangoIndex(int textIndex)
        {
            return Encoding.UTF8.GetByteCount(textIndex < Text.Length ? Text.Remove(textIndex) : Text);
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

        private Size Measure()
        {
            int width;
            int height;
            Layout.GetPixelSize(out width, out height);

            return new Size(width, height);
        }
    }
}
