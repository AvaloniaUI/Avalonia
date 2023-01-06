using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserPlatformSettings : DefaultPlatformSettings
{
    private bool _isDarkMode;
    
    public BrowserPlatformSettings()
    {
        _isDarkMode = DomHelper.ObserveDarkMode(m =>
        {
            _isDarkMode = m;
            OnColorValuesChanged(GetColorValues());
        });
    }
    
    public override PlatformColorValues GetColorValues()
    {
        return base.GetColorValues() with
        {
            ThemeVariant = _isDarkMode ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light
        };
    }
}
