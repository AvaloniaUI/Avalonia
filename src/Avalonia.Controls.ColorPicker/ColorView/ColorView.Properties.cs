using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <inheritdoc/>
    public partial class ColorView
    {
        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<ColorView, Color>(
                nameof(Color),
                Colors.White,
                defaultBindingMode: BindingMode.TwoWay,
                coerce: CoerceColor) ;

        /// <summary>
        /// Defines the <see cref="ColorModel"/> property.
        /// </summary>
        public static readonly StyledProperty<ColorModel> ColorModelProperty =
            AvaloniaProperty.Register<ColorView, ColorModel>(
                nameof(ColorModel),
                ColorModel.Rgba);

        /// <summary>
        /// Defines the <see cref="ColorSpectrumComponents"/> property.
        /// </summary>
        public static readonly StyledProperty<ColorSpectrumComponents> ColorSpectrumComponentsProperty =
            AvaloniaProperty.Register<ColorView, ColorSpectrumComponents>(
                nameof(ColorSpectrumComponents),
                ColorSpectrumComponents.HueSaturation);

        /// <summary>
        /// Defines the <see cref="ColorSpectrumShape"/> property.
        /// </summary>
        public static readonly StyledProperty<ColorSpectrumShape> ColorSpectrumShapeProperty =
            AvaloniaProperty.Register<ColorView, ColorSpectrumShape>(
                nameof(ColorSpectrumShape),
                ColorSpectrumShape.Box);

        /// <summary>
        /// Defines the <see cref="HexInputAlphaPosition"/> property.
        /// </summary>
        public static readonly StyledProperty<AlphaComponentPosition> HexInputAlphaPositionProperty =
            AvaloniaProperty.Register<ColorView, AlphaComponentPosition>(
                nameof(HexInputAlphaPosition),
                AlphaComponentPosition.Leading); // By default match XAML and the WinUI control

        /// <summary>
        /// Defines the <see cref="HsvColor"/> property.
        /// </summary>
        public static readonly StyledProperty<HsvColor> HsvColorProperty =
            AvaloniaProperty.Register<ColorView, HsvColor>(
                nameof(HsvColor),
                Colors.White.ToHsv(),
                defaultBindingMode: BindingMode.TwoWay,
                coerce: CoerceHsvColor);

        /// <summary>
        /// Defines the <see cref="IsAccentColorsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAccentColorsVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsAccentColorsVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsAlphaEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAlphaEnabledProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsAlphaEnabled),
                true);

        /// <summary>
        /// Defines the <see cref="IsAlphaVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAlphaVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsAlphaVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsColorComponentsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsColorComponentsVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsColorComponentsVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsColorModelVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsColorModelVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsColorModelVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsColorPaletteVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsColorPaletteVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsColorPaletteVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsColorPreviewVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsColorPreviewVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsColorPreviewVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsColorSpectrumVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsColorSpectrumVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsColorSpectrumVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsColorSpectrumSliderVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsColorSpectrumSliderVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsColorSpectrumSliderVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsComponentSliderVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsComponentSliderVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsComponentSliderVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsComponentTextInputVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsComponentTextInputVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsComponentTextInputVisible),
                true);

        /// <summary>
        /// Defines the <see cref="IsHexInputVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsHexInputVisibleProperty =
            AvaloniaProperty.Register<ColorView, bool>(
                nameof(IsHexInputVisible),
                true);

        /// <summary>
        /// Defines the <see cref="MaxHue"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxHueProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(MaxHue),
                359);

        /// <summary>
        /// Defines the <see cref="MaxSaturation"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxSaturationProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(MaxSaturation),
                100);

        /// <summary>
        /// Defines the <see cref="MaxValue"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxValueProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(MaxValue),
                100);

        /// <summary>
        /// Defines the <see cref="MinHue"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MinHueProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(MinHue),
                0);

        /// <summary>
        /// Defines the <see cref="MinSaturation"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MinSaturationProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(MinSaturation),
                0);

        /// <summary>
        /// Defines the <see cref="MinValue"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MinValueProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(MinValue),
                0);

        /// <summary>
        /// Defines the <see cref="PaletteColors"/> property.
        /// </summary>
        public static readonly StyledProperty<IEnumerable<Color>?> PaletteColorsProperty =
            AvaloniaProperty.Register<ColorView, IEnumerable<Color>?>(
                nameof(PaletteColors),
                null);

        /// <summary>
        /// Defines the <see cref="PaletteColumnCount"/> property.
        /// </summary>
        public static readonly StyledProperty<int> PaletteColumnCountProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(PaletteColumnCount),
                4);

        /// <summary>
        /// Defines the <see cref="Palette"/> property.
        /// </summary>
        public static readonly StyledProperty<IColorPalette?> PaletteProperty =
            AvaloniaProperty.Register<ColorView, IColorPalette?>(
                nameof(Palette),
                null);

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly StyledProperty<int> SelectedIndexProperty =
            AvaloniaProperty.Register<ColorView, int>(
                nameof(SelectedIndex),
                (int)ColorViewTab.Spectrum);

        /// <inheritdoc cref="ColorSpectrum.Color"/>
        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <inheritdoc cref="ColorSlider.ColorModel"/>
        /// <remarks>
        /// This property is only applicable to the components tab.
        /// The spectrum tab must always be in HSV and the palette tab contains only pre-defined colors.
        /// </remarks>
        public ColorModel ColorModel
        {
            get => GetValue(ColorModelProperty);
            set => SetValue(ColorModelProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.Components"/>
        public ColorSpectrumComponents ColorSpectrumComponents
        {
            get => GetValue(ColorSpectrumComponentsProperty);
            set => SetValue(ColorSpectrumComponentsProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.Shape"/>
        public ColorSpectrumShape ColorSpectrumShape
        {
            get => GetValue(ColorSpectrumShapeProperty);
            set => SetValue(ColorSpectrumShapeProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of the alpha component in the hexadecimal input box relative to
        /// all other color components.
        /// </summary>
        public AlphaComponentPosition HexInputAlphaPosition
        {
            get => GetValue(HexInputAlphaPositionProperty);
            set => SetValue(HexInputAlphaPositionProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.HsvColor"/>
        public HsvColor HsvColor
        {
            get => GetValue(HsvColorProperty);
            set => SetValue(HsvColorProperty, value);
        }

        /// <inheritdoc cref="ColorPreviewer.IsAccentColorsVisible"/>
        public bool IsAccentColorsVisible
        {
            get => GetValue(IsAccentColorsVisibleProperty);
            set => SetValue(IsAccentColorsVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the alpha component is enabled.
        /// When disabled (set to false) the alpha component will be fixed to maximum and
        /// editing controls disabled.
        /// </summary>
        public bool IsAlphaEnabled
        {
            get => GetValue(IsAlphaEnabledProperty);
            set => SetValue(IsAlphaEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the alpha component editing controls
        /// (Slider(s) and TextBox) are visible. When hidden, the existing alpha component
        /// value is maintained.
        /// </summary>
        /// <remarks>
        /// Note that <see cref="IsComponentTextInputVisible"/> also controls the alpha
        /// component TextBox visibility.
        /// </remarks>
        public bool IsAlphaVisible
        {
            get => GetValue(IsAlphaVisibleProperty);
            set => SetValue(IsAlphaVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the color components tab/panel/page (subview) is visible.
        /// </summary>
        public bool IsColorComponentsVisible
        {
            get => GetValue(IsColorComponentsVisibleProperty);
            set => SetValue(IsColorComponentsVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the active color model indicator/selector is visible.
        /// </summary>
        public bool IsColorModelVisible
        {
            get => GetValue(IsColorModelVisibleProperty);
            set => SetValue(IsColorModelVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the color palette tab/panel/page (subview) is visible.
        /// </summary>
        public bool IsColorPaletteVisible
        {
            get => GetValue(IsColorPaletteVisibleProperty);
            set => SetValue(IsColorPaletteVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the color preview is visible.
        /// </summary>
        /// <remarks>
        /// Note that accent color visibility is controlled separately by
        /// <see cref="IsAccentColorsVisible"/>.
        /// </remarks>
        public bool IsColorPreviewVisible
        {
            get => GetValue(IsColorPreviewVisibleProperty);
            set => SetValue(IsColorPreviewVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the color spectrum tab/panel/page (subview) is visible.
        /// </summary>
        public bool IsColorSpectrumVisible
        {
            get => GetValue(IsColorSpectrumVisibleProperty);
            set => SetValue(IsColorSpectrumVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the color spectrum's third component slider
        /// is visible.
        /// </summary>
        public bool IsColorSpectrumSliderVisible
        {
            get => GetValue(IsColorSpectrumSliderVisibleProperty);
            set => SetValue(IsColorSpectrumSliderVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether color component sliders are visible.
        /// </summary>
        /// <remarks>
        /// All color components are controlled by this property but alpha can also be
        /// controlled with <see cref="IsAlphaVisible"/>.
        /// </remarks>
        public bool IsComponentSliderVisible
        {
            get => GetValue(IsComponentSliderVisibleProperty);
            set => SetValue(IsComponentSliderVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether color component text inputs are visible.
        /// </summary>
        /// <remarks>
        /// All color components are controlled by this property but alpha can also be
        /// controlled with <see cref="IsAlphaVisible"/>.
        /// </remarks>
        public bool IsComponentTextInputVisible
        {
            get => GetValue(IsComponentTextInputVisibleProperty);
            set => SetValue(IsComponentTextInputVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the hexadecimal color value text input
        /// is visible.
        /// </summary>
        public bool IsHexInputVisible
        {
            get => GetValue(IsHexInputVisibleProperty);
            set => SetValue(IsHexInputVisibleProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.MaxHue"/>
        public int MaxHue
        {
            get => GetValue(MaxHueProperty);
            set => SetValue(MaxHueProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.MaxSaturation"/>
        public int MaxSaturation
        {
            get => GetValue(MaxSaturationProperty);
            set => SetValue(MaxSaturationProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.MaxValue"/>
        public int MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.MinHue"/>
        public int MinHue
        {
            get => GetValue(MinHueProperty);
            set => SetValue(MinHueProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.MinSaturation"/>
        public int MinSaturation
        {
            get => GetValue(MinSaturationProperty);
            set => SetValue(MinSaturationProperty, value);
        }

        /// <inheritdoc cref="ColorSpectrum.MinValue"/>
        public int MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of individual colors in the palette.
        /// </summary>
        /// <remarks>
        /// This is not commonly set manually. Instead, it should be set automatically by
        /// providing an <see cref="IColorPalette"/> to the <see cref="Palette"/> property.
        /// <br/><br/>
        /// Also note that this property is what should be bound in the control template.
        /// <see cref="Palette"/> is too high-level to use on its own.
        /// </remarks>
        public IEnumerable<Color>? PaletteColors
        {
            get => GetValue(PaletteColorsProperty);
            set => SetValue(PaletteColorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of colors in each row (section) of the color palette.
        /// Within a standard palette, rows are shades and columns are colors.
        /// </summary>
        /// <remarks>
        /// This is not commonly set manually. Instead, it should be set automatically by
        /// providing an <see cref="IColorPalette"/> to the <see cref="Palette"/> property.
        /// <br/><br/>
        /// Also note that this property is what should be bound in the control template.
        /// <see cref="Palette"/> is too high-level to use on its own.
        /// </remarks>
        public int PaletteColumnCount
        {
            get => GetValue(PaletteColumnCountProperty);
            set => SetValue(PaletteColumnCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the color palette.
        /// </summary>
        /// <remarks>
        /// This will automatically set both <see cref="PaletteColors"/> and
        /// <see cref="PaletteColumnCount"/> overwriting any existing values.
        /// </remarks>
        public IColorPalette? Palette
        {
            get => GetValue(PaletteProperty);
            set => SetValue(PaletteProperty, value);
        }

        /// <summary>
        /// Gets or sets the index of the selected tab/panel/page (subview).
        /// </summary>
        /// <remarks>
        /// When using the default control theme, this property is designed to be used with the
        /// <see cref="ColorViewTab"/> enum. The <see cref="ColorViewTab"/> enum defines the
        /// index values of each of the three standard tabs.
        /// Use like `SelectedIndex = (int)ColorViewTab.Palette`.
        /// </remarks>
        public int SelectedIndex
        {
            get => GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }
    }
}
