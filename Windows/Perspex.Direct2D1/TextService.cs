// -----------------------------------------------------------------------
// <copyright file="TextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
    using System.Linq;
    using Perspex.Media;
    using Perspex.Platform;
    using SharpDX.DirectWrite;

    public class TextService : ITextService
    {
        private Factory factory;

        public TextService(Factory factory)
        {
            this.factory = factory;
        }

        public static TextFormat GetTextFormat(Factory factory, FormattedText text)
        {
            return new TextFormat(
                factory,
                text.FontFamilyName,
                FontWeight.Normal,
                (SharpDX.DirectWrite.FontStyle)text.FontStyle,
                (float)text.FontSize);
        }

        public TextLayout GetTextLayout(Factory factory, FormattedText text, Size constraint)
        {
            return new TextLayout(
                factory,
                text.Text,
                GetTextFormat(factory, text),
                (float)constraint.Width,
                (float)constraint.Height);
        }

        public int GetCaretIndex(FormattedText text, Point point, Size constraint)
        {
            using (TextLayout layout = GetTextLayout(this.factory, text, constraint))
            {
                SharpDX.Bool isTrailingHit;
                SharpDX.Bool isInside;

                HitTestMetrics result = layout.HitTestPoint(
                    (float)point.X,
                    (float)point.Y,
                    out isTrailingHit,
                    out isInside);

                return result.TextPosition + (isTrailingHit ? 1 : 0);
            }
        }

        public Point GetCaretPosition(FormattedText text, int caretIndex, Size constraint)
        {
            using (TextLayout layout = GetTextLayout(this.factory, text, constraint))
            {
                float x;
                float y;
                layout.HitTestTextPosition(caretIndex, false, out x, out y);
                return new Point(x, y);
            }
        }

        public double[] GetLineHeights(FormattedText text, Size constraint)
        {
            using (TextLayout layout = GetTextLayout(this.factory, text, constraint))
            {
                return layout.GetLineMetrics().Select(x => (double)x.Height).ToArray();
            }
        }

        public Size Measure(FormattedText text, Size constraint)
        {
            using (TextLayout layout = GetTextLayout(this.factory, text, constraint))
            {
                return new Size(
                    layout.Metrics.WidthIncludingTrailingWhitespace,
                    layout.Metrics.Height);
            }
        }
    }
}
