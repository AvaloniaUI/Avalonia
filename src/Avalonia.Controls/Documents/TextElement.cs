using Avalonia.Media;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// TextElement is an  base class for content in text based controls.
    /// TextElements span other content, applying property values or providing structural information.
    /// </summary>
    public abstract class TextElement : StyledElement
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextElement>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontFamily>(
                nameof(FontFamily),
                defaultValue: FontFamily.Default,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontFeatures"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFeatureCollection?> FontFeaturesProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontFeatureCollection?>(
                nameof(FontFeatures),
                inherits: true);
        
        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, double>(
                nameof(FontSize),
                defaultValue: 12,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="FontStretch"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStretch> FontStretchProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontStretch>(
                nameof(FontStretch),
                inherits: true,
                defaultValue: FontStretch.Normal);

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush?> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, IBrush?>(
                nameof(Foreground),
                Brushes.Black,
                inherits: true);

        private IInlineHost? _inlineHost;

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font features.
        /// </summary>
        public FontFeatureCollection? FontFeatures
        {
            get => GetValue(FontFeaturesProperty);
            set => SetValue(FontFeaturesProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the font stretch.
        /// </summary>
        public FontStretch FontStretch
        {
            get => GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
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
        /// Sets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontFamily(Control control, FontFamily value)
        {
            control.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontFeaturesProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontFeatureCollection? GetFontFeatures(Control control)
        {
            return control.GetValue(FontFeaturesProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFeaturesProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontFeatures(Control control, FontFeatureCollection? value)
        {
            control.SetValue(FontFeaturesProperty, value);
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
        /// Sets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontSize(Control control, double value)
        {
            control.SetValue(FontSizeProperty, value);
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
        /// Sets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontStyle(Control control, FontStyle value)
        {
            control.SetValue(FontStyleProperty, value);
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
        /// Sets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontWeight(Control control, FontWeight value)
        {
            control.SetValue(FontWeightProperty, value);
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
        /// Sets the value of the attached <see cref="FontStretchProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontStretch(Control control, FontStretch value)
        {
            control.SetValue(FontStretchProperty, value);
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
        /// Sets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetForeground(Control control, IBrush? value)
        {
            control.SetValue(ForegroundProperty, value);
        }

        internal IInlineHost? InlineHost
        {
            get => _inlineHost;
            set
            {
                var oldValue = _inlineHost;
                _inlineHost = value;
                OnInlineHostChanged(oldValue, value);
            }
        }

        internal virtual void OnInlineHostChanged(IInlineHost? oldValue, IInlineHost? newValue)
        {

        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(Background):
                case nameof(FontFamily):
                case nameof(FontSize):
                case nameof(FontStyle):
                case nameof(FontWeight):
                case nameof(FontStretch):
                case nameof(Foreground):
                    InlineHost?.Invalidate();
                    break;
            }
        }
    }
}
