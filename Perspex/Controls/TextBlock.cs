// -----------------------------------------------------------------------
// <copyright file="TextBlock.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Media;
    using Splat;

    public class TextBlock : Control
    {
        public static readonly PerspexProperty<double> FontSizeProperty =
            PerspexProperty.Register<TextBlock, double>(
                "FontSize",
                defaultValue: 12.0,
                inherits: true);

        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<Border, string>("Text");

        public TextBlock()
        {
            this.GetObservable(TextProperty).Subscribe(_ => this.InvalidateVisual());
        }

        public double FontSize
        {
            get { return this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
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
            if (this.Visibility == Visibility.Visible)
            {
                Brush background = this.Background;

                if (background != null)
                {
                    context.FillRectange(background, this.Bounds);
                }

                context.DrawText(this.Foreground, new Rect(this.Bounds.Size), this.FormattedText);
            }
        }

        protected override Size MeasureContent(Size availableSize)
        {
            if (this.Visibility != Visibility.Collapsed)
            {
                ITextService service = Locator.Current.GetService<ITextService>();

                if (!string.IsNullOrEmpty(this.Text))
                {
                    return service.Measure(this.FormattedText);
                }
            }

            return new Size();
        }
    }
}
