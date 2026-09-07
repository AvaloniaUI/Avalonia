using System;
using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserPlatformSettings : DefaultPlatformSettings
{
    private bool _isDarkMode;
    private bool _isHighContrast;
    private bool _isInitialized;
    private PlatformColorValues? _colorValues;
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
        if (_colorValues is null)
        {
            EnsureSettings();
            _colorValues = BuildPlatformColorValues(_isDarkMode, _isHighContrast);
        }

        return _colorValues;
    }

    private static PlatformColorValues BuildPlatformColorValues(bool isDarkMode, bool isHighContrast)
        => new()
        {
            ThemeVariant = isDarkMode ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light,
            ContrastPreference = isHighContrast ? ColorContrastPreference.High : ColorContrastPreference.NoPreference
        };

    public void OnColorValuesChanged(bool isDarkMode, bool isHighContrast)
    {
        _isDarkMode = isDarkMode;
        _isHighContrast = isHighContrast;
        UpdateColorValues();
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

    private void UpdateColorValues()
    {
        var oldColorValues = _colorValues;
        var colorValues = BuildPlatformColorValues(_isDarkMode, _isHighContrast);

        if (oldColorValues != colorValues)
        {
            _colorValues = colorValues;
            OnColorValuesChanged(colorValues);
        }
    }
}
