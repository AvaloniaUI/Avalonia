using Avalonia.Platform;

namespace Avalonia.UnitTests;

public class TestPlatformSettings(PlatformThemeVariant themeVariant)
    : DefaultPlatformSettings
{
    private PlatformColorValues _colorValues = new() { ThemeVariant = themeVariant };

    public override PlatformColorValues GetColorValues() => _colorValues;

    public PlatformThemeVariant ThemeVariant
    {
        get => _colorValues.ThemeVariant;
        set => SetColorValues(_colorValues with { ThemeVariant = value });
    }

    public void SetColorValues(PlatformColorValues colorValues)
    {
        _colorValues = colorValues;
        OnColorValuesChanged(colorValues);
    }
}
