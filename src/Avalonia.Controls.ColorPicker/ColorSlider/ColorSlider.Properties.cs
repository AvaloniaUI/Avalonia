using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <inheritdoc/>
    public partial class ColorSlider
    {
        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<ColorSlider, Color>(
                nameof(Color),
                Colors.White,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="ColorComponent"/> property.
        /// </summary>
        public static readonly StyledProperty<ColorComponent> ColorComponentProperty =
            AvaloniaProperty.Register<ColorSlider, ColorComponent>(
                nameof(ColorComponent),
                ColorComponent.Component1);

        /// <summary>
        /// Defines the <see cref="ColorModel"/> property.
        /// </summary>
        public static readonly StyledProperty<ColorModel> ColorModelProperty =
            AvaloniaProperty.Register<ColorSlider, ColorModel>(
                nameof(ColorModel),
                ColorModel.Rgba);

        /// <summary>
        /// Defines the <see cref="HsvColor"/> property.
        /// </summary>
        public static readonly StyledProperty<HsvColor> HsvColorProperty =
            AvaloniaProperty.Register<ColorSlider, HsvColor>(
                nameof(HsvColor),
                Colors.White.ToHsv(),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsAlphaMaxForced"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAlphaMaxForcedProperty =
            AvaloniaProperty.Register<ColorSlider, bool>(
                nameof(IsAlphaMaxForced),
                true);

        /// <summary>
        /// Defines the <see cref="IsRoundingEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsRoundingEnabledProperty =
            AvaloniaProperty.Register<ColorSlider, bool>(
                nameof(IsRoundingEnabled),
                false);

        /// <summary>
        /// Defines the <see cref="IsSaturationValueMaxForced"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSaturationValueMaxForcedProperty =
            AvaloniaProperty.Register<ColorSlider, bool>(
                nameof(IsSaturationValueMaxForced),
                true);

        /// <summary>
        /// Gets or sets the currently selected color in the RGB color model.
        /// </summary>
        /// <remarks>
        /// Use this property instead of <see cref="HsvColor"/> when in <see cref="ColorModel.Rgba"/>
        /// to avoid loss of precision and color drifting.
        /// </remarks>
        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color component represented by the slider.
        /// </summary>
        public ColorComponent ColorComponent
        {
            get => GetValue(ColorComponentProperty);
            set => SetValue(ColorComponentProperty, value);
        }

        /// <summary>
        /// Gets or sets the active color model used by the slider.
        /// </summary>
        public ColorModel ColorModel
        {
            get => GetValue(ColorModelProperty);
            set => SetValue(ColorModelProperty, value);
        }

        /// <summary>
        /// Gets or sets the currently selected color in the HSV color model.
        /// </summary>
        /// <remarks>
        /// Use this property instead of <see cref="Color"/> when in <see cref="ColorModel.Hsva"/>
        /// to avoid loss of precision and color drifting.
        /// </remarks>
        public HsvColor HsvColor
        {
            get => GetValue(HsvColorProperty);
            set => SetValue(HsvColorProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the alpha component is always forced to maximum for components
        /// other than <see cref="ColorComponent"/>.
        /// This ensures that the background is always visible and never transparent regardless of the actual color.
        /// </summary>
        public bool IsAlphaMaxForced
        {
            get => GetValue(IsAlphaMaxForcedProperty);
            set => SetValue(IsAlphaMaxForcedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether rounding of color component values is enabled.
        /// </summary>
        /// <remarks>
        /// This is applicable for the HSV color model only. The <see cref="Media.HsvColor"/> struct uses double
        /// values while the <see cref="Media.Color"/> struct uses byte. Only double types need rounding.
        /// </remarks>
        public bool IsRoundingEnabled
        {
            get => GetValue(IsRoundingEnabledProperty);
            set => SetValue(IsRoundingEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the saturation and value components are always forced to maximum values
        /// when using the HSVA color model. Only component values other than <see cref="ColorComponent"/> will be changed.
        /// This ensures, for example, that the Hue background is always visible and never washed out regardless of the actual color.
        /// </summary>
        public bool IsSaturationValueMaxForced
        {
            get => GetValue(IsSaturationValueMaxForcedProperty);
            set => SetValue(IsSaturationValueMaxForcedProperty, value);
        }
    }
}
