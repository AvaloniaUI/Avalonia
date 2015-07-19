// -----------------------------------------------------------------------
// <copyright file="FormattedText.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using System.Collections.Generic;
    using Perspex.Platform;
    using Splat;

    public class FormattedText : IDisposable
    {
        public FormattedText(
            string text,
            string fontFamilyName,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            this.Text = text;
            this.FontFamilyName = fontFamilyName;
            this.FontSize = fontSize;
            this.FontStyle = fontStyle;
            this.FontWeight = fontWeight;
            this.TextAlignment = textAlignment;

            var platform = Locator.Current.GetService<IPlatformRenderInterface>();

            this.PlatformImpl = platform.CreateFormattedText(
                text, 
                fontFamilyName, 
                fontSize, 
                fontStyle, 
                textAlignment,
                fontWeight);
        }

        public Size Constraint
        {
            get { return this.PlatformImpl.Constraint; }
            set { this.PlatformImpl.Constraint = value; }
        }

        public string FontFamilyName
        {
            get;
            private set;
        }

        public double FontSize
        {
            get;
            private set;
        }

        public FontStyle FontStyle
        {
            get;
            private set;
        }

        public FontWeight FontWeight
        {
            get;
            private set;
        }

        public string Text
        {
            get;
            private set;
        }

        public IFormattedTextImpl PlatformImpl
        {
            get;
            private set;
        }

        public TextAlignment TextAlignment
        {
            get;
            private set;
        }

        public void Dispose()
        {
            this.PlatformImpl.Dispose();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return this.PlatformImpl.GetLines();
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            return this.PlatformImpl.HitTestPoint(point);
        }

        public Rect HitTestTextPosition(int index)
        {
            return this.PlatformImpl.HitTestTextPosition(index);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length, Point origin = default(Point))
        {
            return this.PlatformImpl.HitTestTextRange(index, length, origin);
        }

        public Size Measure()
        {
            return this.PlatformImpl.Measure();
        }

        public void SetForegroundBrush(Brush brush, int startIndex, int count)
        {
            this.PlatformImpl.SetForegroundBrush(brush, startIndex, count);
        }
    }
} 
