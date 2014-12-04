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
        public static readonly PerspexProperty<double> FontSizeProperty =
            PerspexProperty.Register<Control, double>(
                "FontSize",
                defaultValue: 12.0,
                inherits: true);

        public static readonly PerspexProperty<FontStyle> FontStyleProperty =
            PerspexProperty.Register<Control, FontStyle>("FontStyle", inherits: true);

        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<TextBlock, string>("Text");

        static TextBlock()
        {
            Control.AffectsMeasure(TextProperty);
        }

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public double FontSize
        {
            get { return this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return this.GetValue(FontStyleProperty); }
            set { this.SetValue(FontStyleProperty, value); }
        }

        private FormattedText FormattedText
        {
            get
            {
                return new FormattedText
                {
                    FontFamilyName = "Segoe UI",
                    FontSize = this.FontSize,
                    FontStyle = this.FontStyle,
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
                ITextService textService = Locator.Current.GetService<ITextService>();
                return textService.Measure(this.FormattedText, availableSize);
            }

            return new Size();
        }
    }
}
