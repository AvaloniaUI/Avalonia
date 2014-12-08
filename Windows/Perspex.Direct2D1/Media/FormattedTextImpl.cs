// -----------------------------------------------------------------------
// <copyright file="TextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;
    using DWrite = SharpDX.DirectWrite;

    public class FormattedTextImpl : IFormattedTextImpl
    {
        public FormattedTextImpl(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle)
        {
            var factory = Locator.Current.GetService<DWrite.Factory>();

            this.TextLayout = new DWrite.TextLayout(
                factory,
                text ?? string.Empty,
                new DWrite.TextFormat(factory, fontFamily, (float)fontSize),
                float.MaxValue,
                float.MaxValue);
        }

        public Size Constraint
        {
            get
            {
                return new Size(this.TextLayout.MaxWidth, this.TextLayout.MaxHeight);
            }

            set
            {
                this.TextLayout.MaxWidth = (float)value.Width;
                this.TextLayout.MaxHeight = (float)value.Height;
            }
        }

        public DWrite.TextLayout TextLayout
        {
            get;
            private set;
        }

        public void Dispose()
        {
            this.TextLayout.Dispose();
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            SharpDX.Bool isTrailingHit;
            SharpDX.Bool isInside;

            DWrite.HitTestMetrics result = this.TextLayout.HitTestPoint(
                (float)point.X,
                (float)point.Y,
                out isTrailingHit,
                out isInside);

            return new TextHitTestResult
            {
                TextPosition = result.TextPosition,
                IsTrailing = isTrailingHit,
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            float x;
            float y;

            DWrite.HitTestMetrics result = this.TextLayout.HitTestTextPosition(
                index, 
                false, 
                out x, 
                out y);

            return new Rect(result.Left, result.Top, result.Width, result.Height);
        }

        public Size Measure()
        {
            return new Size(
                this.TextLayout.Metrics.WidthIncludingTrailingWhitespace,
                this.TextLayout.Metrics.Height);
        }
    }
}
