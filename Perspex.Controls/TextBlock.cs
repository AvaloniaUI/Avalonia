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

        private FormattedText formattedText = new FormattedText();

        public TextBlock()
        {
            this.GetObservable(TextProperty).Subscribe(x =>
            {
                this.formattedText.Text = x;
                this.InvalidateMeasure();
            });

            this.GetObservable(FontSizeProperty).Subscribe(x =>
            {
                this.formattedText.FontSize = x;
                this.InvalidateMeasure();
            });

            this.GetObservable(FontStyleProperty).Subscribe(x =>
            {
                this.formattedText.FontStyle = x;
                this.InvalidateMeasure();
            });
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

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, new Rect(this.ActualSize));
            }

            context.DrawText(
                this.Foreground, 
                new Rect(this.ActualSize), 
                this.formattedText);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!string.IsNullOrEmpty(this.Text))
            {
                this.formattedText.Constraint = availableSize;
                return this.formattedText.Measure();
            }

            return new Size();
        }
    }
}
