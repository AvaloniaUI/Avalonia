using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <inheritdoc/>
    public partial class ColorPreviewer
    {
        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<ColorSpectrum, Color>(
                nameof(Color),
                Colors.White);

        /// <summary>
        /// Gets or sets the currently previewed color in the RGB color model.
        /// </summary>
        /// <remarks>
        /// For control authors use <see cref="HsvColor"/> instead to avoid loss
        /// of precision and color drifting.
        /// </remarks>
        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="HsvColor"/> property.
        /// </summary>
        public static readonly StyledProperty<HsvColor> HsvColorProperty =
            AvaloniaProperty.Register<ColorPreviewer, HsvColor>(
                nameof(HsvColor),
                Colors.Transparent.ToHsv());

        /// <summary>
        /// Gets or sets the currently previewed color in the HSV color model.
        /// </summary>
        /// <remarks>
        /// This should be used in all cases instead of the <see cref="Color"/> property.
        /// Internally, the <see cref="ColorPreviewer"/> uses the HSV color model and using
        /// this property will avoid loss of precision and color drifting.
        /// </remarks>
        public HsvColor HsvColor
        {
            get => GetValue(HsvColorProperty);
            set => SetValue(HsvColorProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="ShowAccentColors"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowAccentColorsProperty =
            AvaloniaProperty.Register<ColorPreviewer, bool>(
                nameof(ShowAccentColors),
                true);

        /// <summary>
        /// Gets or sets a value indicating whether accent colors are shown along
        /// with the preview color.
        /// </summary>
        public bool ShowAccentColors
        {
            get => (bool)this.GetValue(ShowAccentColorsProperty);
            set => SetValue(ShowAccentColorsProperty, value);
        }
    }
}
