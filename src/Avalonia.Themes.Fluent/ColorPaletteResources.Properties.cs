using Avalonia.Media;

namespace Avalonia.Themes.Fluent;

public partial class ColorPaletteResources
{
    private bool _hasAccentColor;
    private Color _accentColor;
    private Color _accentColorDark1, _accentColorDark2, _accentColorDark3;
    private Color _accentColorLight1, _accentColorLight2, _accentColorLight3;

    public static readonly DirectProperty<ColorPaletteResources, Color> AccentProperty
        = AvaloniaProperty.RegisterDirect<ColorPaletteResources, Color>(nameof(Accent), r => r.Accent, (r, v) => r.Accent = v);

    /// <summary>
    /// Gets or sets the Accent color value.
    /// </summary>
    public Color Accent
    {
        get => _accentColor;
        set => SetAndRaise(AccentProperty, ref _accentColor, value);
    }

    /// <summary>
    /// Gets or sets the AltHigh color value.
    /// </summary>
    public Color AltHigh { get => GetColor("SystemAltHighColor"); set => SetColor("SystemAltHighColor", value); }

    /// <summary>
    /// Gets or sets the AltLow color value.
    /// </summary>
    public Color AltLow { get => GetColor("SystemAltLowColor"); set => SetColor("SystemAltLowColor", value); }

    /// <summary>
    /// Gets or sets the AltMedium color value.
    /// </summary>
    public Color AltMedium { get => GetColor("SystemAltMediumColor"); set => SetColor("SystemAltMediumColor", value); }

    /// <summary>
    /// Gets or sets the AltMediumHigh color value.
    /// </summary>
    public Color AltMediumHigh { get => GetColor("SystemAltMediumHighColor"); set => SetColor("SystemAltMediumHighColor", value); }

    /// <summary>
    /// Gets or sets the AltMediumLow color value.
    /// </summary>
    public Color AltMediumLow { get => GetColor("SystemAltMediumLowColor"); set => SetColor("SystemAltMediumLowColor", value); }

    /// <summary>
    /// Gets or sets the BaseHigh color value.
    /// </summary>
    public Color BaseHigh { get => GetColor("SystemBaseHighColor"); set => SetColor("SystemBaseHighColor", value); }

    /// <summary>
    /// Gets or sets the BaseLow color value.
    /// </summary>
    public Color BaseLow { get => GetColor("SystemBaseLowColor"); set => SetColor("SystemBaseLowColor", value); }

    /// <summary>
    /// Gets or sets the BaseMedium color value.
    /// </summary>
    public Color BaseMedium { get => GetColor("SystemBaseMediumColor"); set => SetColor("SystemBaseMediumColor", value); }

    /// <summary>
    /// Gets or sets the BaseMediumHigh color value.
    /// </summary>
    public Color BaseMediumHigh { get => GetColor("SystemBaseMediumHighColor"); set => SetColor("SystemBaseMediumHighColor", value); }

    /// <summary>
    /// Gets or sets the BaseMediumLow color value.
    /// </summary>
    public Color BaseMediumLow { get => GetColor("SystemBaseMediumLowColor"); set => SetColor("SystemBaseMediumLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeAltLow color value.
    /// </summary>
    public Color ChromeAltLow { get => GetColor("SystemChromeAltLowColor"); set => SetColor("SystemChromeAltLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeBlackHigh color value.
    /// </summary>
    public Color ChromeBlackHigh { get => GetColor("SystemChromeBlackHighColor"); set => SetColor("SystemChromeBlackHighColor", value); }

    /// <summary>
    /// Gets or sets the ChromeBlackLow color value.
    /// </summary>
    public Color ChromeBlackLow { get => GetColor("SystemChromeBlackLowColor"); set => SetColor("SystemChromeBlackLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeBlackMedium color value.
    /// </summary>
    public Color ChromeBlackMedium { get => GetColor("SystemChromeBlackMediumColor"); set => SetColor("SystemChromeBlackMediumColor", value); }

    /// <summary>
    /// Gets or sets the ChromeBlackMediumLow color value.
    /// </summary>
    public Color ChromeBlackMediumLow { get => GetColor("SystemChromeBlackMediumLowColor"); set => SetColor("SystemChromeBlackMediumLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeDisabledHigh color value.
    /// </summary>
    public Color ChromeDisabledHigh { get => GetColor("SystemChromeDisabledHighColor"); set => SetColor("SystemChromeDisabledHighColor", value); }

    /// <summary>
    /// Gets or sets the ChromeDisabledLow color value.
    /// </summary>
    public Color ChromeDisabledLow { get => GetColor("SystemChromeDisabledLowColor"); set => SetColor("SystemChromeDisabledLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeGray color value.
    /// </summary>
    public Color ChromeGray { get => GetColor("SystemChromeGrayColor"); set => SetColor("SystemChromeGrayColor", value); }

    /// <summary>
    /// Gets or sets the ChromeHigh color value.
    /// </summary>
    public Color ChromeHigh { get => GetColor("SystemChromeHighColor"); set => SetColor("SystemChromeHighColor", value); }

    /// <summary>
    /// Gets or sets the ChromeLow color value.
    /// </summary>
    public Color ChromeLow { get => GetColor("SystemChromeLowColor"); set => SetColor("SystemChromeLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeMedium color value.
    /// </summary>
    public Color ChromeMedium { get => GetColor("SystemChromeMediumColor"); set => SetColor("SystemChromeMediumColor", value); }

    /// <summary>
    /// Gets or sets the ChromeMediumLow color value.
    /// </summary>
    public Color ChromeMediumLow { get => GetColor("SystemChromeMediumLowColor"); set => SetColor("SystemChromeMediumLowColor", value); }

    /// <summary>
    /// Gets or sets the ChromeWhite color value.
    /// </summary>
    public Color ChromeWhite { get => GetColor("SystemChromeWhiteColor"); set => SetColor("SystemChromeWhiteColor", value); }

    /// <summary>
    /// Gets or sets the ErrorText color value.
    /// </summary>
    public Color ErrorText { get => GetColor("SystemErrorTextColor"); set => SetColor("SystemErrorTextColor", value); }

    /// <summary>
    /// Gets or sets the ListLow color value.
    /// </summary>
    public Color ListLow { get => GetColor("SystemListLowColor"); set => SetColor("SystemListLowColor", value); }

    /// <summary>
    /// Gets or sets the ListMedium color value.
    /// </summary>
    public Color ListMedium { get => GetColor("SystemListMediumColor"); set => SetColor("SystemListMediumColor", value); }
    
    /// <summary>
    /// Gets or sets the RegionColor color value.
    /// </summary>
    public Color RegionColor { get => GetColor("SystemRegionColor"); set => SetColor("SystemRegionColor", value); }
}
