using System;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using MiniMvvm;

namespace ControlCatalog.ViewModels;

public class PlatformSettingsViewModel : ViewModelBase
{
    private IPlatformSettings? _platformSettings;
    private PlatformColorValues? _colorValues;
    private string? _preferredLanguage;

    public void Subscribe(IPlatformSettings? platformSettings)
    {
        _platformSettings = platformSettings;

        if (platformSettings is not null)
        {
            OnColorValuesChanged(platformSettings, platformSettings.GetColorValues());
            OnPreferredLanguageChanged(platformSettings, EventArgs.Empty);

            platformSettings?.ColorValuesChanged += OnColorValuesChanged;
            platformSettings?.PreferredApplicationLanguageChanged += OnPreferredLanguageChanged;
        }
    }

    public void Unsubscribe()
    {
        if (_platformSettings != null)
        {
            _platformSettings.ColorValuesChanged -= OnColorValuesChanged;
            _platformSettings.PreferredApplicationLanguageChanged -= OnPreferredLanguageChanged;
            _platformSettings = null;
        }
    }

    private void OnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        _colorValues = e;
        RaisePropertyChanged(nameof(ThemeVariant));
        RaisePropertyChanged(nameof(ContrastPreference));
        RaisePropertyChanged(nameof(AccentColor1));
        RaisePropertyChanged(nameof(AccentColor2));
        RaisePropertyChanged(nameof(AccentColor3));
    }

    private void OnPreferredLanguageChanged(object? sender, EventArgs e)
    {
        _preferredLanguage = ((IPlatformSettings)sender!).PreferredApplicationLanguage;
        RaisePropertyChanged(nameof(PreferredLanguage));
    }

    public string PreferredLanguage => _preferredLanguage ?? "Not available";

    public string ThemeVariant => _colorValues?.ThemeVariant.ToString() ?? "Not available";

    public string ContrastPreference => _colorValues?.ContrastPreference.ToString() ?? "Not available";

    public Color AccentColor1 => _colorValues?.AccentColor1 ?? Colors.Gray;

    public Color AccentColor2 => _colorValues?.AccentColor2 ?? Colors.Gray;

    public Color AccentColor3 => _colorValues?.AccentColor3 ?? Colors.Gray;

    public string HoldWaitDuration => _platformSettings?.HoldWaitDuration.ToString() ?? "Not available";

    public string TapSizeTouch => _platformSettings?.GetTapSize(PointerType.Touch).ToString() ?? "Not available";

    public string TapSizeMouse => _platformSettings?.GetTapSize(PointerType.Mouse).ToString() ?? "Not available";

    public string DoubleTapSizeTouch => _platformSettings?.GetDoubleTapSize(PointerType.Touch).ToString() ?? "Not available";

    public string DoubleTapSizeMouse => _platformSettings?.GetDoubleTapSize(PointerType.Mouse).ToString() ?? "Not available";

    public string DoubleTapTimeTouch => _platformSettings?.GetDoubleTapTime(PointerType.Touch).ToString() ?? "Not available";

    public string DoubleTapTimeMouse => _platformSettings?.GetDoubleTapTime(PointerType.Mouse).ToString() ?? "Not available";
}
