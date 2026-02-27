using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserPlatformSettings : DefaultPlatformSettings
{
    private bool _isDarkMode;
    private bool _isHighContrast;
    private bool _isInitialized;
    private string? _lastLanguage;

    public override event EventHandler<PlatformColorValues>? ColorValuesChanged
    {
        add
        {
            EnsureSettings();
            base.ColorValuesChanged += value;
        }
        remove => base.ColorValuesChanged -= value;
    }

    public override event EventHandler? PreferredApplicationLanguageChanged
    {
        add
        {
            EnsureSettings();
            base.PreferredApplicationLanguageChanged += value;
        }
        remove => base.PreferredApplicationLanguageChanged -= value;
    }

    public override string PreferredApplicationLanguage
    {
        get
        {
            EnsureSettings();

            return _lastLanguage ?? base.PreferredApplicationLanguage;
        }
    }

    public override PlatformColorValues GetColorValues()
    {
        EnsureSettings();

        return base.GetColorValues() with
        {
            ThemeVariant = _isDarkMode ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light,
            ContrastPreference = _isHighContrast ? ColorContrastPreference.High : ColorContrastPreference.NoPreference
        };
    }

    public void OnColorValuesChanged(bool isDarkMode, bool isHighContrast)
    {
        _isDarkMode = isDarkMode;
        _isHighContrast = isHighContrast;
        OnColorValuesChanged(GetColorValues());
    }

    public void OnPreferredLanguageChanged(string? language)
    {
        if (language is not null && _lastLanguage != language)
        {
            _lastLanguage = language;
            OnPreferredApplicationLanguageChanged();   
        }
    }

    private void EnsureSettings()
    {
        if (!_isInitialized)
        {
            // WASM module has async nature of initialization. We can't call platform code right away during components registration. 
            _isInitialized = true;
            var values = DomHelper.GetDarkMode(BrowserWindowingPlatform.GlobalThis);
            if (values.Length == 2)
            {
                _isDarkMode = values[0] > 0;
                _isHighContrast = values[1] > 0;
            }

            _lastLanguage = DomHelper.GetNavigatorLanguage(BrowserWindowingPlatform.GlobalThis);
        }
    }
}
