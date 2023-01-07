using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserPlatformSettings : DefaultPlatformSettings
{
    private bool _isDarkMode;
    private bool _isHighContrast;
    
    public BrowserPlatformSettings()
    {
        var obj = DomHelper.ObserveDarkMode((isDarkMode, isHighContrast) =>
        {
            _isDarkMode = isDarkMode;
            _isHighContrast = isHighContrast;
            OnColorValuesChanged(GetColorValues());
        });
        _isDarkMode = obj.GetPropertyAsBoolean("isDarkMode");
        _isHighContrast = obj.GetPropertyAsBoolean("isHighContrast");
    }

    public override PlatformColorValues GetColorValues()
    {
        return base.GetColorValues() with
        {
            ThemeVariant = _isDarkMode ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light,
            ContrastPreference = _isHighContrast ? ColorContrastPreference.High : ColorContrastPreference.NoPreference
        };
    }
}
