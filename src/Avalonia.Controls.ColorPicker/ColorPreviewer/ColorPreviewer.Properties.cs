using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <inheritdoc/>
    public partial class ColorPreviewer
    {
        /// <summary>
        /// Defines the <see cref="HsvColor"/> property.
        /// </summary>
        public static readonly StyledProperty<HsvColor> HsvColorProperty =
            AvaloniaProperty.Register<ColorPreviewer, HsvColor>(
                nameof(HsvColor),
                Colors.Transparent.ToHsv(),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsAccentColorsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAccentColorsVisibleProperty =
            AvaloniaProperty.Register<ColorPreviewer, bool>(
                nameof(IsAccentColorsVisible),
                true);

        /// <summary>
        /// Gets or sets the currently previewed color in the HSV color model.
        /// </summary>
        /// <remarks>
        /// Only an HSV color is supported in this control to ensure there is never any
        /// loss of precision or color information. Accent colors, like the color spectrum,
        /// only operate with the HSV color model.
        /// </remarks>
        public HsvColor HsvColor
        {
            get => GetValue(HsvColorProperty);
            set => SetValue(HsvColorProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether accent colors are visible along
        /// with the preview color.
        /// </summary>
        public bool IsAccentColorsVisible
        {
            get => GetValue(IsAccentColorsVisibleProperty);
            set => SetValue(IsAccentColorsVisibleProperty, value);
        }
    }
}
