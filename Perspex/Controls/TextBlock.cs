// -----------------------------------------------------------------------
// <copyright file="TextBlock.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;

    public class TextBlock : Control
    {
        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<TextBlock, string>("Text");

        static TextBlock()
        {
            AffectsMeasure(TextProperty);
        }

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        private FormattedText FormattedText
        {
            get
            {
                return new FormattedText
                {
                    FontFamilyName = "Segoe UI",
                    FontSize = this.FontSize,
                    Text = this.Text,
                };
            }
        }

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, new Rect(this.ActualSize));
            }

            context.DrawText(this.Foreground, new Rect(this.ActualSize), this.FormattedText);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!string.IsNullOrEmpty(this.Text))
            {
                return this.FormattedText.Measure(availableSize);
            }

            return new Size();
        }
    }
}
