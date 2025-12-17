using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

// TODO: ideally should be created per view/activity.
internal class PlatformSettings : DefaultPlatformSettings
{
    private readonly NSObject _contentSizeChangedToken;
    private readonly Dictionary<double, double> _fontScaleCache = [];

    private PlatformColorValues? _lastColorValues;

    public PlatformSettings()
    {
        _contentSizeChangedToken = UIApplication.Notifications.ObserveContentSizeCategoryChanged(OnContentSizeCategoryChanged);
    }

    ~PlatformSettings()
    {
        _contentSizeChangedToken.Dispose();
    }

    private void OnContentSizeCategoryChanged(object? sender, UIContentSizeCategoryChangedEventArgs e)
    {
        _fontScaleCache.Clear();
        OnTextScaleChanged();
    }

    public override PlatformColorValues GetColorValues()
    {
        var themeVariant = UITraitCollection.CurrentTraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark ?
            PlatformThemeVariant.Dark :
            PlatformThemeVariant.Light;


        var contrastPreference = UITraitCollection.CurrentTraitCollection.AccessibilityContrast == UIAccessibilityContrast.High ?
            ColorContrastPreference.High :
            ColorContrastPreference.NoPreference;

        UIColor? tintColor = null;
        if (OperatingSystem.IsIOSVersionAtLeast(14))
        {
            tintColor = UIConfigurationColorTransformer.PreferredTint(UIColor.Clear);
        }

        if (tintColor is not null)
        {
            tintColor.GetRGBA(out var red, out var green, out var blue, out var alpha);
            if (red != 0 && green != 0 && blue != 0 && alpha != 0)
            {
                return _lastColorValues = new PlatformColorValues
                {
                    ThemeVariant = themeVariant,
                    ContrastPreference = contrastPreference,
                    AccentColor1 = new Color(
                        (byte)(alpha * 255),
                        (byte)(red * 255),
                        (byte)(green * 255),
                        (byte)(blue * 255))
                };
            }
        }

        return _lastColorValues = new PlatformColorValues
        {
            ThemeVariant = themeVariant, ContrastPreference = contrastPreference
        };
    }

    public void TraitCollectionDidChange()
    {
        var oldColorValues = _lastColorValues;
        var colorValues = GetColorValues();

        if (oldColorValues != colorValues)
        {
            OnColorValuesChanged(colorValues);
        }
    }

    public override double GetScaledFontSize(double baseFontSize)
    {
        if (baseFontSize <= 0)
        {
            return baseFontSize;
        }

        if (!_fontScaleCache.TryGetValue(baseFontSize, out var scaledSize))
        {
            var font = UIFont.SystemFontOfSize((nfloat)baseFontSize);
            scaledSize = UIFontMetrics.DefaultMetrics.GetScaledFont(font).PointSize;
        }

        return scaledSize;
    }
}
