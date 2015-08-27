// -----------------------------------------------------------------------
// <copyright file="TextBlock.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Media;

    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    public class TextBlock : Control
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly PerspexProperty<string> FontFamilyProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, string>(
                nameof(FontFamily),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> FontSizeProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(FontSize),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly PerspexProperty<FontStyle> FontStyleProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly PerspexProperty<FontWeight> FontWeightProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.RegisterAttached<TextBlock, Control, Brush>(
                nameof(Foreground),
                new SolidColorBrush(0xff000000),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<TextBlock, string>(nameof(Text));

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly PerspexProperty<TextAlignment> TextAlignmentProperty =
            PerspexProperty.Register<TextBlock, TextAlignment>(nameof(TextAlignment));

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly PerspexProperty<TextWrapping> TextWrappingProperty =
            PerspexProperty.Register<TextBlock, TextWrapping>(nameof(TextWrapping));

        /// <summary>
        /// The formatted text used for rendering.
        /// </summary>
        private FormattedText formattedText;

        /// <summary>
        /// Stores the last constraint passed to MeasureOverride.
        /// </summary>
        private Size constraint;

        /// <summary>
        /// Initializes static members of the <see cref="TextBlock"/> class.
        /// </summary>
        static TextBlock()
        {
            AffectsRender(ForegroundProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBlock"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public string FontFamily
        {
            get { return this.GetValue(FontFamilyProperty); }
            set { this.SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get { return this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return this.GetValue(FontStyleProperty); }
            set { this.SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return this.GetValue(FontWeightProperty); }
            set { this.SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        public FormattedText FormattedText
        {
            get
            {
                if (this.formattedText == null)
                {
                    this.formattedText = this.CreateFormattedText(this.constraint);
                }

                return this.formattedText;
            }
        }

        /// <summary>
        /// Gets or sets the control's text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return this.GetValue(TextWrappingProperty); }
            set { this.SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return this.GetValue(TextAlignmentProperty); }
            set { this.SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// Renders the <see cref="TextBlock"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, new Rect(this.Bounds.Size));
            }

            this.FormattedText.Constraint = this.Bounds.Size;
            context.DrawText(this.Foreground, new Point(), this.FormattedText);
        }

        /// <summary>
        /// Creates the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        protected virtual FormattedText CreateFormattedText(Size constraint)
        {
            var result = new FormattedText(
                this.Text,
                this.FontFamily,
                this.FontSize,
                this.FontStyle,
                this.TextAlignment,
                this.FontWeight);
            result.Constraint = constraint;
            return result;
        }

        /// <summary>
        /// Invalidates <see cref="FormattedText"/>.
        /// </summary>
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

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>The desired size.</returns>
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