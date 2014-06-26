// -----------------------------------------------------------------------
// <copyright file="TextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
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
                (float)text.FontSize);
        }

        public TextLayout GetTextLayout(Factory factory, FormattedText text)
        {
            return new TextLayout(
                factory,
                text.Text,
                GetTextFormat(factory, text),
                float.MaxValue,
                float.MaxValue);
        }

        public int GetCaretIndex(FormattedText text, Point point)
        {
            TextLayout layout = GetTextLayout(this.factory, text);
            SharpDX.Bool isTrailingHit;
            SharpDX.Bool isInside;

            HitTestMetrics result = layout.HitTestPoint(
                (float)point.X,
                (float)point.Y,
                out isTrailingHit,
                out isInside);

            return result.TextPosition + (isTrailingHit ? 1 : 0);
        }

        public Point GetCaretPosition(FormattedText text, int caretIndex)
        {
            TextLayout layout = GetTextLayout(this.factory, text);
            float x;
            float y;
            layout.HitTestTextPosition(caretIndex, false, out x, out y);
            return new Point(x, y);
        }

        public Size Measure(FormattedText text)
        {
            TextLayout layout = GetTextLayout(this.factory, text);
            return new Size(
                layout.Metrics.WidthIncludingTrailingWhitespace,
                layout.Metrics.Height);
        }
    }
}
