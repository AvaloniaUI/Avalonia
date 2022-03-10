using System;
using System.Collections.Generic;
using System.Text;
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
    public class TextBlock : Control
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

        // TODO: Define these attached properties elsewhere (e.g. on a Text class) and AddOwner
        // them into TextBlock.

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, FontFamily>(
                nameof(FontFamily),
                defaultValue: FontFamily.Default,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(FontSize),
                defaultValue: 12,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="FontStretch"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStretch> FontStretchProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, FontStretch>(
                nameof(FontStretch),
                inherits: true,
                defaultValue: FontStretch.Normal);
        
        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush?> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, IBrush?>(
                nameof(Foreground),
                Brushes.Black,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="LineHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LineHeightProperty =
            AvaloniaProperty.Register<TextBlock, double>(
                nameof(LineHeight),
                double.NaN,
                validate: IsValidLineHeight);

        /// <summary>
        /// Defines the <see cref="MaxLines"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxLinesProperty =
            AvaloniaProperty.Register<TextBlock, int>(
                nameof(MaxLines),
                validate: IsValidMaxLines);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<TextBlock, string?> TextProperty =
            AvaloniaProperty.RegisterDirect<TextBlock, string?>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        /// <summary>
        /// Defines the <see cref="Inlines"/> property.
        /// </summary>
        public static readonly DirectProperty<TextBlock, InlineCollection> InlinesProperty =
            AvaloniaProperty.RegisterDirect<TextBlock, InlineCollection>(
                nameof(Inlines),
                o => o.Inlines);

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

        /// <summary>
        /// Defines the <see cref="TextTrimming"/> property.
        /// </summary>
        public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
            AvaloniaProperty.Register<TextBlock, TextTrimming>(nameof(TextTrimming));

        /// <summary>
        /// Defines the <see cref="TextDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
            AvaloniaProperty.Register<TextBlock, TextDecorationCollection?>(nameof(TextDecorations));

        private TextLayout? _textLayout;
        private Size _constraint;

        /// <summary>
        /// Initializes static members of the <see cref="TextBlock"/> class.
        /// </summary>
        static TextBlock()
        {
            ClipToBoundsProperty.OverrideDefaultValue<TextBlock>(true);
            
            AffectsRender<TextBlock>(BackgroundProperty, ForegroundProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBlock"/> class.
        /// </summary>
        public TextBlock()
        {
            Inlines = new InlineCollection(this);

            Inlines.Invalidated += InlinesChanged;
        }

        /// <summary>
        /// Gets the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        public TextLayout TextLayout
        {
            get
            {
                return _textLayout ?? (_textLayout = CreateTextLayout(_constraint, Text));
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
            get => Inlines.Text;
            set
            {
                var old = Text;

                if (value == old)
                {
                    return;
                }

                Inlines.Text = value;

                RaisePropertyChanged(TextProperty, old, value);
            }
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection Inlines { get; }

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
        /// Gets or sets the font stretch.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
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
        /// <returns>The font size.</returns>
        public static double GetFontSize(Control control)
        {
            return control.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font style.</returns>
        public static FontStyle GetFontStyle(Control control)
        {
            return control.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font weight.</returns>
        public static FontWeight GetFontWeight(Control control)
        {
            return control.GetValue(FontWeightProperty);
        }
        
        /// <summary>
        /// Gets the value of the attached <see cref="FontStretchProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font stretch.</returns>
        public static FontStretch GetFontStretch(Control control)
        {
            return control.GetValue(FontStretchProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The foreground.</returns>
        public static IBrush? GetForeground(Control control)
        {
            return control.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontFamily(Control control, FontFamily value)
        {
            control.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontSize(Control control, double value)
        {
            control.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontStyle(Control control, FontStyle value)
        {
            control.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontWeight(Control control, FontWeight value)
        {
            control.SetValue(FontWeightProperty, value);
        }
        
        /// <summary>
        /// Sets the value of the attached <see cref="FontStretchProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontStretch(Control control, FontStretch value)
        {
            control.SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetForeground(Control control, IBrush? value)
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

            TextLayout.Draw(context, new Point(padding.Left, top));
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected virtual TextLayout CreateTextLayout(Size constraint, string? text)
        {
            List<ValueSpan<TextRunProperties>>? textStyleOverrides = null;

            if (Inlines.HasComplexContent)
            {
                textStyleOverrides = new List<ValueSpan<TextRunProperties>>(Inlines.Count);

                var textPosition = 0;
                var stringBuilder = new StringBuilder();

                foreach (var inline in Inlines)
                {
                    textPosition += inline.BuildRun(stringBuilder, textStyleOverrides, textPosition);
                }

                text = stringBuilder.ToString();
            }

            return new TextLayout(
                text ?? string.Empty,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize,
                Foreground ?? Brushes.Transparent,
                TextAlignment,
                TextWrapping,
                TextTrimming,
                TextDecorations,
                FlowDirection,
                constraint.Width,
                constraint.Height,
                maxLines: MaxLines,
                lineHeight: LineHeight,
                textStyleOverrides: textStyleOverrides);
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
            if (!Inlines.HasComplexContent && string.IsNullOrEmpty(Text))
            {
                return new Size();
            }

            var padding = Padding;
            
            _constraint = availableSize.Deflate(padding);
            
            _textLayout = null;

            InvalidateArrange();

            var measuredSize = PixelSize.FromSize(TextLayout.Bounds.Size, 1);

            return new Size(measuredSize.Width, measuredSize.Height).Inflate(padding);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (MathUtilities.AreClose(_constraint.Width, finalSize.Width))
            {
                return finalSize;
            }
            
            _constraint = finalSize;
            
            _textLayout = null;

            return finalSize;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockAutomationPeer(this);
        }

        private static bool IsValidMaxLines(int maxLines) => maxLines >= 0;

        private static bool IsValidLineHeight(double lineHeight) => double.IsNaN(lineHeight) || lineHeight > 0;

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof (FontSize):
                case nameof (FontWeight):
                case nameof (FontStyle):
                case nameof (FontFamily):

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

 		private void InlinesChanged(object? sender, EventArgs e)
        {
            InvalidateTextLayout();
        }
    }
}
