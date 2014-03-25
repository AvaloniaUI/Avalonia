// -----------------------------------------------------------------------
// <copyright file="TextBlock.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Media;
    using Splat;

    public class TextBlock : Control
    {
        public static readonly PerspexProperty<double> FontSizeProperty =
            PerspexProperty.Register<TextBlock, double>(
                "FontSize",
                inherits: true);

        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<Border, string>("Text");

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
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, this.Bounds);
            }

            context.DrawText(this.Foreground, new Rect(this.Bounds.Size), this.FormattedText);
        }

        protected override Size MeasureContent(Size availableSize)
        {
            ITextService service = Locator.Current.GetService<ITextService>();
            return service.Measure(this.FormattedText);
        }
    }
}
