// -----------------------------------------------------------------------
// <copyright file="FormattedText.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    public enum FontStyle
    {
        Normal,
        Oblique,
        Italic,
    }

    public class FormattedText
    {
        private IFormattedTextImpl impl;

        public FormattedText()
        {
            this.impl = Locator.Current.GetService<IFormattedTextImpl>();
        }

        public Size Constraint
        {
            get { return this.impl.Constraint; }
            set { this.impl.Constraint = value; }
        }

        public string FontFamilyName
        {
            get { return this.impl.FontFamilyName; }
            set { this.impl.FontFamilyName = value; }
        }

        public double FontSize
        {
            get { return this.impl.FontSize; }
            set { this.impl.FontSize = value; }
        }

        public FontStyle FontStyle
        {
            get { return this.impl.FontStyle; }
            set { this.impl.FontStyle = value; }
        }

        public string Text
        {
            get { return this.impl.Text; }
            set { this.impl.Text = value; }
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            return this.impl.HitTestPoint(point);
        }

        public Rect HitTestTextPosition(int index)
        {
            return this.impl.HitTestTextPosition(index);
        }

        public Size Measure()
        {
            return this.impl.Measure();
        }
    }
}
