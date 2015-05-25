// -----------------------------------------------------------------------
// <copyright file="TextBlock.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Media;

    public class TextBlock : Control
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        public static readonly PerspexProperty<string> FontFamilyProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, string>("FontFamily", inherits: true);

        public static readonly PerspexProperty<double> FontSizeProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, double>("FontSize", inherits: true);

        public static readonly PerspexProperty<FontStyle> FontStyleProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, FontStyle>("FontStyle", inherits: true);

        public static readonly PerspexProperty<FontWeight> FontWeightProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, FontWeight>("FontWeight", inherits: true, defaultValue: FontWeight.Normal);

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, Brush>("Foreground", new SolidColorBrush(0xff000000), inherits: true);

        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<TextBlock, string>("Text");

        public static readonly PerspexProperty<TextWrapping> TextWrappingProperty =
            PerspexProperty.Register<TextBlock, TextWrapping>("TextWrapping");

        public static readonly PerspexProperty<TextAlignment> TextAlignmentProperty =
            PerspexProperty.Register<TextBlock, TextAlignment>("TextAlignment");

        private FormattedText formattedText;

        private Size constraint;

        public TextBlock()
        {
            Observable.Merge(
                this.GetObservable(TextProperty).Select(_ => Unit.Default),
                this.GetObservable(TextAlignmentProperty).Select(_ => Unit.Default),
                this.GetObservable(FontSizeProperty).Select(_ => Unit.Default),
                this.GetObservable(FontStyleProperty).Select(_ => Unit.Default))
                .Subscribe(_ =>
                {
                    this.InvalidateFormattedText();
                });
        }

        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public string FontFamily
        {
            get { return this.GetValue(FontFamilyProperty); }
            set { this.SetValue(FontFamilyProperty, value); }
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

        public FontWeight FontWeight
        {
            get { return this.GetValue(FontWeightProperty); }
            set { this.SetValue(FontWeightProperty, value); }
        }

        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public FormattedText FormattedText
        {
            get
            {
                if (this.formattedText == null)
                {
                    this.formattedText = this.CreateFormattedText();
                }

                return this.formattedText;
            }
        }

        public TextWrapping TextWrapping
        {
            get { return this.GetValue(TextWrappingProperty); }
            set { this.SetValue(TextWrappingProperty, value); }
        }

        public TextAlignment TextAlignment
        {
            get { return this.GetValue(TextAlignmentProperty); }
            set { this.SetValue(TextAlignmentProperty, value); }
        }

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, new Rect(this.Bounds.Size));
            }

            this.FormattedText.Constraint = this.Bounds.Size;
            context.DrawText(this.Foreground,  new Point(), this.FormattedText);
        }

        protected virtual FormattedText CreateFormattedText()
        {
            var result = new FormattedText(
                this.Text,
                this.FontFamily,
                this.FontSize,
                this.FontStyle,
                this.TextAlignment,
                this.FontWeight);
            result.Constraint = this.constraint;
            return result;
        }

        protected void InvalidateFormattedText()
        {
            if (this.formattedText != null)
            {
                this.constraint = this.formattedText.Constraint;
                this.formattedText.Dispose();
                this.formattedText = null;
            }

            this.InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!string.IsNullOrEmpty(this.Text))
            {
                if (this.TextWrapping == TextWrapping.Wrap)
                {
                    this.FormattedText.Constraint = new Size(availableSize.Width, double.PositiveInfinity);
                }
                else
                {
                    this.FormattedText.Constraint = Size.Infinity;
                }

                return this.FormattedText.Measure();
            }

            return new Size();
        }
    }
}
