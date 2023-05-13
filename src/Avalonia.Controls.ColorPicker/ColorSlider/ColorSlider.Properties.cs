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
        /// Defines the <see cref="IsAlphaVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAlphaVisibleProperty =
            AvaloniaProperty.Register<ColorSlider, bool>(
                nameof(IsAlphaVisible),
                false);

        /// <summary>
        /// Defines the <see cref="IsPerceptive"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsPerceptiveProperty =
            AvaloniaProperty.Register<ColorSlider, bool>(
                nameof(IsPerceptive),
                true);

        /// <summary>
        /// Defines the <see cref="IsRoundingEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsRoundingEnabledProperty =
            AvaloniaProperty.Register<ColorSlider, bool>(
                nameof(IsRoundingEnabled),
                false);

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
        /// Gets or sets a value indicating whether the alpha component is visible and rendered.
        /// When false, this ensures that the gradient is always visible and never transparent regardless of
        /// the actual color. This property is ignored when the alpha component itself is being displayed.
        /// </summary>
        /// <remarks>
        /// Setting to false means the alpha component is always forced to maximum for components other than
        /// <see cref="ColorComponent"/> during rendering. This doesn't change the value of the alpha component
        /// in the color – it is only for display.
        /// </remarks>
        public bool IsAlphaVisible
        {
            get => GetValue(IsAlphaVisibleProperty);
            set => SetValue(IsAlphaVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the slider adapts rendering to improve user-perception
        /// over exactness.
        /// </summary>
        /// <remarks>
        /// When true in the HSVA color model, this ensures that the gradient is always visible and
        /// never washed out regardless of the actual color. When true in the RGBA color model, this ensures
        /// the gradient always appears as red, green or blue.
        /// <br/><br/>
        /// For example, with Hue in the HSVA color model, the Saturation and Value components are always forced
        /// to maximum values during rendering. In the RGBA color model, all components other than
        /// <see cref="ColorComponent"/> are forced to minimum values during rendering.
        /// <br/><br/>
        /// Note this property will only adjust components other than <see cref="ColorComponent"/> during rendering.
        /// This also doesn't change the values of any components in the actual color – it is only for display.
        /// </remarks>
        public bool IsPerceptive
        {
            get => GetValue(IsPerceptiveProperty);
            set => SetValue(IsPerceptiveProperty, value);
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
    }
}
