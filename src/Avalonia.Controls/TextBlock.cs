// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    [ContentWrapper(typeof(Run))]
    public class TextBlock : Control
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, FontFamily>(
                nameof(FontFamily),
                defaultValue:  FontFamily.Default,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<TextBlock, string> TextProperty =
            AvaloniaProperty.RegisterDirect<TextBlock, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.Register<TextBlock, TextAlignment>(nameof(TextAlignment));

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.Register<TextBlock, TextWrapping>(nameof(TextWrapping));

        private FormattedText _formattedText;
        private Size _constraint;

        /// <summary>
        /// Initializes static members of the <see cref="TextBlock"/> class.
        /// </summary>
        static TextBlock()
        {
            ClipToBoundsProperty.OverrideDefaultValue<TextBlock>(true);
            AffectsRender<TextBlock>(
                BackgroundProperty,
                ForegroundProperty,
                FontWeightProperty,
                FontSizeProperty,
                FontStyleProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBlock"/> class.
        /// </summary>
        public TextBlock()
        {
            Inlines.ForEachItem(
                x => LogicalChildren.Add(x),
                x => LogicalChildren.Remove(x),
                () => throw new NotSupportedException());
            Inlines.Invalidated += InlinesChanged;
            AffectsFormattedText<TextBlock>(
                TextProperty,
                TextAlignmentProperty,
                FontSizeProperty,
                FontStyleProperty,
                FontWeightProperty);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text
        {
            get => Inlines.Text;
            set
            {
                var old = Text;
                Inlines.Text = value;

                if (Text != old)
                {
                    RaisePropertyChanged(TextProperty, old, value);
                }
            }
        }

        /// <summary>
        /// Gets a collection of inline text elements to display.
        /// </summary>
        [Content]
        public InlineCollection Inlines { get; } = new InlineCollection();

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        public FormattedText FormattedText
        {
            get
            {
                if (_formattedText == null)
                {
                    _formattedText = CreateFormattedText(_constraint);
                }

                return _formattedText;
            }
        }

        /// <summary>
        /// Gets or sets the control's text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontFamily GetFontFamily(Control control)
        {
            return control.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static double GetFontSize(Control control)
        {
            return control.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontStyle GetFontStyle(Control control)
        {
            return control.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontWeight GetFontWeight(Control control)
        {
            return control.GetValue(FontWeightProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The foreground.</returns>
        public static IBrush GetForeground(Control control)
        {
            return control.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontFamily(Control control, FontFamily value)
        {
            control.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontSize(Control control, double value)
        {
            control.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontStyle(Control control, FontStyle value)
        {
            control.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontWeight(Control control, FontWeight value)
        {
            control.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetForeground(Control control, IBrush value)
        {
            control.SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Renders the <see cref="TextBlock"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var background = Background;

            if (background != null)
            {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }

            FormattedText.Constraint = Bounds.Size;
            context.DrawText(Foreground, new Point(), FormattedText);
        }

        protected static void AffectsFormattedText<T>(params AvaloniaProperty[] properties)
            where T : TextBlock
        {
            void Handler(AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Sender is T i)
                {
                    i.InvalidateFormattedText();
                    i.InvalidateMeasure();
                }
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(Handler);
            }
        }

        /// <summary>
        /// Creates the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        protected virtual FormattedText CreateFormattedText(Size constraint)
        {
            var builder = new FormattedTextBuilder();

            foreach (var i in Inlines)
            {
                i.BuildFormattedText(builder);
            }

            var result = builder.ToFormattedText();
            result.Constraint = constraint;
            result.TextAlignment = TextAlignment;
            result.Typeface = new Typeface(FontFamily, FontStyle, FontWeight);
            result.FontSize = FontSize;
            result.Wrapping = TextWrapping;
            return result;
        }

        /// <summary>
        /// Creates a <see cref="FormattedText"/> instance using the specified text and constraint
        /// and using the base styling properties of the <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        [Obsolete("Remove this")]
        protected FormattedText CreateFormattedText(Size constraint, string text)
        {
            return new FormattedText
            {
                Constraint = constraint,
                Typeface = new Typeface(FontFamily, FontStyle, FontWeight),
                FontSize = FontSize,
                Text = text ?? string.Empty,
                TextAlignment = TextAlignment,
                Wrapping = TextWrapping,
            };
        }

        /// <summary>
        /// Invalidates <see cref="FormattedText"/>.
        /// </summary>
        protected void InvalidateFormattedText()
        {
            if (_formattedText != null)
            {
                _constraint = _formattedText.Constraint;
                _formattedText = null;
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (!string.IsNullOrEmpty(Text))
            {
                if (TextWrapping == TextWrapping.Wrap)
                {
                    FormattedText.Constraint = new Size(availableSize.Width, double.PositiveInfinity);
                }
                else
                {
                    FormattedText.Constraint = Size.Infinity;
                }

                return FormattedText.Measure();
            }

            return new Size();
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            InvalidateFormattedText();
            InvalidateMeasure();
        }

        private void InlinesChanged(object sender, EventArgs e)
        {
            InvalidateFormattedText();
            InvalidateMeasure();
        }
    }
}
