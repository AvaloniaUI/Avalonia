using System;
using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserPlatformSettings : DefaultPlatformSettings
{
    private bool _isDarkMode;
    private bool _isHighContrast;
    private bool _isInitialized;

    public override event EventHandler<PlatformColorValues>? ColorValuesChanged
    {
        add
        {
            EnsureBackend();
            base.ColorValuesChanged += value;
        }
        remove => base.ColorValuesChanged -= value;
    }

    public override PlatformColorValues GetColorValues()
    {
        EnsureBackend();

        return base.GetColorValues() with
        {
            ThemeVariant = _isDarkMode ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light,
            ContrastPreference = _isHighContrast ? ColorContrastPreference.High : ColorContrastPreference.NoPreference
        };
    }

    public void OnValuesChanged(bool isDarkMode, bool isHighContrast)
    {
        _isDarkMode = isDarkMode;
        _isHighContrast = isHighContrast;
        OnColorValuesChanged(GetColorValues());
    }
    
    private void EnsureBackend()
    {
        if (!_isInitialized)
        {
            // WASM module has async nature of initialization. We can't native code right away during components registration. 
            _isInitialized = true;
            var values = DomHelper.GetDarkMode(BrowserWindowingPlatform.GlobalThis);
            if (values.Length == 2)
            {
                _isDarkMode = values[0] > 0;
                _isHighContrast = values[1] > 0;
            }
        }
    }
}
