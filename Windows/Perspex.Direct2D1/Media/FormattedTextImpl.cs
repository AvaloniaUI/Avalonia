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
        private string text;

        private string fontFamilyName = "Ariel";

        private double fontSize = 10;

        private FontStyle fontStyle;

        private DWrite.Factory factory;

        private DWrite.TextLayout layout;

        public FormattedTextImpl()
        {
            this.factory = Locator.Current.GetService<DWrite.Factory>();
        }

        public Size Constraint
        {
            get
            {
                return new Size(this.Layout.MaxWidth, this.Layout.MaxHeight);
            }

            set
            {
                this.Layout.MaxWidth = (float)value.Width;
                this.Layout.MaxHeight = (float)value.Height;
            }
        }

        public string FontFamilyName
        {
            get
            {
                return this.fontFamilyName;
            }

            set
            {
                if (this.fontFamilyName != value)
                {
                    this.fontFamilyName = value;
                    this.DisposeLayout();
                }
            }
        }

        public double FontSize
        {
            get
            {
                return this.fontSize;
            }

            set
            {
                if (this.fontSize != value)
                {
                    this.fontSize = value;
                    this.DisposeLayout();
                }
            }
        }

        public FontStyle FontStyle
        {
            get
            {
                return this.fontStyle;
            }

            set
            {
                if (this.fontStyle != value)
                {
                    this.fontStyle = value;
                    this.DisposeLayout();
                }
            }
        }

        public string Text
        {
            get
            {
                return this.text;
            }

            set
            {
                if (this.text != value)
                {
                    this.text = value;
                    this.DisposeLayout();
                }
            }
        }

        public DWrite.TextLayout Layout
        {
            get
            {
                if (this.layout == null)
                {
                    this.layout = new DWrite.TextLayout(
                        this.factory,
                        this.text ?? string.Empty,
                        new DWrite.TextFormat(this.factory, this.fontFamilyName, (float)this.fontSize),
                        float.MaxValue,
                        float.MaxValue);
                }

                return this.layout;
            }
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            SharpDX.Bool isTrailingHit;
            SharpDX.Bool isInside;

            DWrite.HitTestMetrics result = layout.HitTestPoint(
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

            DWrite.HitTestMetrics result = layout.HitTestTextPosition(
                index, 
                false, 
                out x, 
                out y);

            return new Rect(result.Left, result.Top, result.Width, result.Height);
        }

        public Size Measure()
        {
            return new Size(
                layout.Metrics.WidthIncludingTrailingWhitespace,
                layout.Metrics.Height);
        }

        private void DisposeLayout()
        {
            if (this.layout != null)
            {
                this.layout.Dispose();
                this.layout = null;
            }
        }
    }
}
