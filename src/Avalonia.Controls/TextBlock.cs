using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    public class TextBlock : Control, IAddChild<string>
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner<TextBlock>();

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
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStretch> FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// DependencyProperty for <see cref="BaselineOffset" /> property.
        /// </summary>
        public static readonly AttachedProperty<double> BaselineOffsetProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(BaselineOffset),
                0,
                true);

        /// <summary>
        /// Defines the <see cref="LineHeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> LineHeightProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(LineHeight),
                double.NaN,
                validate: IsValidLineHeight,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="MaxLines"/> property.
        /// </summary>
        public static readonly AttachedProperty<int> MaxLinesProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, int>(
                nameof(MaxLines),
                validate: IsValidMaxLines,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<TextBlock, string?> TextProperty =
            AvaloniaProperty.RegisterDirect<TextBlock, string?>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, TextAlignment>(
                nameof(TextAlignment), 
                defaultValue: TextAlignment.Start,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, TextWrapping>(nameof(TextWrapping), 
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextTrimming"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextTrimming> TextTrimmingProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, TextTrimming>(nameof(TextTrimming), 
                defaultValue: TextTrimming.None,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
            AvaloniaProperty.Register<TextBlock, TextDecorationCollection?>(nameof(TextDecorations));

        internal string? _text;
        protected TextLayout? _textLayout;
        protected Size _constraint;

        /// <summary>
        /// Initializes static members of the <see cref="TextBlock"/> class.
        /// </summary>
        static TextBlock()
        {
            ClipToBoundsProperty.OverrideDefaultValue<TextBlock>(true);
            
            AffectsRender<TextBlock>(BackgroundProperty, ForegroundProperty);
        }

        /// <summary>
        /// Gets the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        public TextLayout TextLayout
        {
            get
            {
                return _textLayout ??= CreateTextLayout(_text);
            }
        }

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Text"/>.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string? Text
        {
            get => GetText();
            set => SetText(value);
        }

        /// <summary>
        /// Gets or sets the font family used to draw the control's text.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the size of the control's text in points.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style used to draw the control's text.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight used to draw the control's text.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font stretch used to draw the control's text.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's text and other foreground elements.
        /// </summary>
        public IBrush? Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of each line of content.
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of text lines.
        /// </summary>
        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
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
        /// Gets or sets the control's text trimming mode.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get { return GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
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
        /// Gets or sets the text decorations.
        /// </summary>
        public TextDecorationCollection? TextDecorations
        {
            get => GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }
        
        protected override bool BypassFlowDirectionPolicies => true;

        /// <summary>
        /// The BaselineOffset property provides an adjustment to baseline offset
        /// </summary>
        public double BaselineOffset
        {
            get { return (double)GetValue(BaselineOffsetProperty); }
            set { SetValue(BaselineOffsetProperty, value); }
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static double GetBaselineOffset(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(BaselineOffsetProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetBaselineOffset(Control control, double value)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(BaselineOffsetProperty, value);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static TextAlignment GetTextAlignment(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(TextAlignmentProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="alignment">The property value to set</param>
        public static void SetTextAlignment(Control control, TextAlignment alignment)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(TextAlignmentProperty, alignment);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static TextWrapping GetTextWrapping(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(TextWrappingProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="wrapping">The property value to set</param>
        public static void SetTextWrapping(Control control, TextWrapping wrapping)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(TextWrappingProperty, wrapping);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static TextTrimming GetTextTrimming(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(TextTrimmingProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="trimming">The property value to set</param>
        public static void SetTextTrimming(Control control, TextTrimming trimming)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(TextTrimmingProperty, trimming);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static double GetLineHeight(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(LineHeightProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="height">The property value to set</param>
        public static void SetLineHeight(Control control, double height)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(LineHeightProperty, height);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static int GetMaxLines(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(MaxLinesProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="maxLines">The property value to set</param>
        public static void SetMaxLines(Control control, int maxLines)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(MaxLinesProperty, maxLines);
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

            var padding = Padding;
            var top = padding.Top;
            var textHeight = TextLayout.Bounds.Height;

            if (Bounds.Height < textHeight)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        top += (Bounds.Height - textHeight) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        top += (Bounds.Height - textHeight);
                        break;
                }
            }

            RenderTextLayout(context, new Point(padding.Left, top));
        }

        protected virtual void RenderTextLayout(DrawingContext context, Point origin)
        {
            TextLayout.Draw(context, origin);
        }

        void IAddChild<string>.AddChild(string text)
        {
            _text = text;
        }

        protected virtual string? GetText()
        {
            return _text;
        }

        protected virtual void SetText(string? text)
        {
            SetAndRaise(TextProperty, ref _text, text);
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected virtual TextLayout CreateTextLayout(string? text)
        {
            var defaultProperties = new GenericTextRunProperties(
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize,
                TextDecorations,
                Foreground);

            var paragraphProperties = new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false,
                defaultProperties, TextWrapping, LineHeight, 0);

            return new TextLayout(
                new SimpleTextSource((text ?? "").AsMemory(), defaultProperties),
                paragraphProperties,
                TextTrimming,
                _constraint.Width,
                _constraint.Height,
                maxLines: MaxLines,
                lineHeight: LineHeight);
        }

        /// <summary>
        /// Invalidates <see cref="TextLayout"/>.
        /// </summary>
        protected void InvalidateTextLayout()
        {
            _textLayout = null;

            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var scale = LayoutHelper.GetLayoutScale(this);

            var padding = LayoutHelper.RoundLayoutThickness(Padding, scale, scale);

            _constraint = availableSize.Deflate(padding);

            _textLayout = null;

            InvalidateArrange();

            var measuredSize = TextLayout.Bounds.Size.Inflate(padding);

            return measuredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var textWidth = Math.Ceiling(TextLayout.Bounds.Width);

            if(finalSize.Width < textWidth)
            {
                finalSize = finalSize.WithWidth(textWidth);
            }

            if (MathUtilities.AreClose(_constraint.Width, finalSize.Width))
            {
                return finalSize;
            }

            var scale = LayoutHelper.GetLayoutScale(this);

            var padding = LayoutHelper.RoundLayoutThickness(Padding, scale, scale);

            _constraint = new Size(Math.Ceiling(finalSize.Deflate(padding).Width), double.PositiveInfinity);

            _textLayout = null;

            return finalSize;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockAutomationPeer(this);
        }

        private static bool IsValidMaxLines(int maxLines) => maxLines >= 0;

        private static bool IsValidLineHeight(double lineHeight) => double.IsNaN(lineHeight) || lineHeight > 0;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof (FontSize):
                case nameof (FontWeight):
                case nameof (FontStyle):
                case nameof (FontFamily):
                case nameof (FontStretch):

                case nameof (TextWrapping):
                case nameof (TextTrimming):
                case nameof (TextAlignment):

                case nameof (FlowDirection):

                case nameof (Padding):
                case nameof (LineHeight):
                case nameof (MaxLines):

                case nameof (Text):
                case nameof (TextDecorations):
                case nameof (Foreground):
                {
                    InvalidateTextLayout();
                    break;
                }
            }
        }

        protected readonly struct SimpleTextSource : ITextSource
        {
            private readonly ReadOnlySlice<char> _text;
            private readonly TextRunProperties _defaultProperties;

            public SimpleTextSource(ReadOnlySlice<char> text, TextRunProperties defaultProperties)
            {
                _text = text;
                _defaultProperties = defaultProperties;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex > _text.Length)
                {
                    return new TextEndOfParagraph();
                }

                var runText = _text.Skip(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfParagraph();
                }

                return new TextCharacters(runText, _defaultProperties);
            }
        }
    }
}
