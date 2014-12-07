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
        public FormattedText()
        {
            this.PlatformImpl = Locator.Current.GetService<IFormattedTextImpl>();
        }

        public Size Constraint
        {
            get { return this.PlatformImpl.Constraint; }
            set { this.PlatformImpl.Constraint = value; }
        }

        public string FontFamilyName
        {
            get { return this.PlatformImpl.FontFamilyName; }
            set { this.PlatformImpl.FontFamilyName = value; }
        }

        public double FontSize
        {
            get { return this.PlatformImpl.FontSize; }
            set { this.PlatformImpl.FontSize = value; }
        }

        public FontStyle FontStyle
        {
            get { return this.PlatformImpl.FontStyle; }
            set { this.PlatformImpl.FontStyle = value; }
        }

        public string Text
        {
            get { return this.PlatformImpl.Text; }
            set { this.PlatformImpl.Text = value; }
        }

        public IFormattedTextImpl PlatformImpl
        {
            get;
            private set;
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            return this.PlatformImpl.HitTestPoint(point);
        }

        public Rect HitTestTextPosition(int index)
        {
            return this.PlatformImpl.HitTestTextPosition(index);
        }

        public Size Measure()
        {
            return this.PlatformImpl.Measure();
        }
    }
}
