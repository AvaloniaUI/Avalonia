using System;
using Android.Content;
using Android.Content.Res;
using Android.Provider;
using Android.Views;
using Avalonia.Input;
using Avalonia.Platform;
using Color = Avalonia.Media.Color;

namespace Avalonia.Android.Platform;

// TODO: ideally should be created per view/activity.
internal class AndroidPlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues _latestValues;
    private TimeSpan _holdWaitDuration = TimeSpan.FromMilliseconds(300);
    private TimeSpan _doubleTapTime = TimeSpan.FromMilliseconds(500);
    private Size _doubleTapSize = new Size(16,16);
    private Size _tapSize = new Size(10,10);

    public AndroidPlatformSettings()
    {
        _latestValues = base.GetColorValues();
        if (global::Android.App.Application.Context is { } context)
        {
            GetInputConfigValues(context);
        }
    }

    public override PlatformColorValues GetColorValues()
    {
        return _latestValues;
    }

    public override TimeSpan GetDoubleTapTime(PointerType type)
    {
        return type == PointerType.Mouse ? base.GetDoubleTapTime(type) : _doubleTapTime;
    }

    public override Size GetDoubleTapSize(PointerType type)
    {
        return type == PointerType.Mouse ? base.GetDoubleTapSize(type) : _doubleTapSize;
    }

    public override Size GetTapSize(PointerType type)
    {
        return type == PointerType.Mouse ? base.GetTapSize(type) : _tapSize;
    }

    public override TimeSpan HoldWaitDuration => _holdWaitDuration;

    internal void OnViewConfigurationChanged(Context context)
    {
        if (context.Resources?.Configuration is null)
        {
            return;
        }

        var systemTheme = (context.Resources.Configuration.UiMode & UiMode.NightMask) switch
        {
            UiMode.NightYes => PlatformThemeVariant.Dark,
            UiMode.NightNo => PlatformThemeVariant.Light,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            // See https://developer.android.com/reference/android/R.color
            var accent1 = context.Resources.GetColor(17170494, context.Theme); // Resource.Color.SystemAccent1500
            var accent2 = context.Resources.GetColor(17170507, context.Theme); // Resource.Color.SystemAccent2500
            var accent3 = context.Resources.GetColor(17170520, context.Theme); // Resource.Color.SystemAccent3500

            _latestValues = new PlatformColorValues
            {
                ThemeVariant = systemTheme,
                ContrastPreference = IsHighContrast(context),
                AccentColor1 = new Color(accent1.A, accent1.R, accent1.G, accent1.B),
                AccentColor2 = new Color(accent2.A, accent2.R, accent2.G, accent2.B),
                AccentColor3 = new Color(accent3.A, accent3.R, accent3.G, accent3.B),
            };
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            // See https://developer.android.com/reference/android/R.attr
            var array = context.Theme?.ObtainStyledAttributes(new[] { 16843829 }); // Resource.Attribute.ColorAccent
            if (array is not null)
            {
                try
                {
                    var accent = array.GetColor(0, 0);

                    _latestValues = new PlatformColorValues
                    {
                        ThemeVariant = systemTheme,
                        ContrastPreference = IsHighContrast(context),
                        AccentColor1 = new Color(accent.A, accent.R, accent.G, accent.B)
                    };
                }
                finally
                {
                    array.Recycle();   
                }
            }
        }
        else
        {
            _latestValues = _latestValues with { ThemeVariant = systemTheme };
        }

        GetInputConfigValues(context);

        OnColorValuesChanged(_latestValues);
    }

    private void GetInputConfigValues(Context context)
    {
        _holdWaitDuration = TimeSpan.FromMilliseconds(ViewConfiguration.LongPressTimeout);

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            _doubleTapTime = TimeSpan.FromMilliseconds(ViewConfiguration.MultiPressTimeout);
        }
        var config = ViewConfiguration.Get(context);
        var scaling = context.Resources?.DisplayMetrics?.Density ?? 1;
        if (config != null)
        {
            var size = config.ScaledDoubleTapSlop * 2 / scaling;
            _doubleTapSize = new Size(size, size);
            size = config.ScaledTouchSlop * 2 / scaling;
            _tapSize = new Size(size, size);
        }
    }

    private static ColorContrastPreference IsHighContrast(Context context)
    {
        try
        {
            return Settings.Secure.GetInt(context.ContentResolver, "high_text_contrast_enabled", 0) == 1
                ? ColorContrastPreference.High : ColorContrastPreference.NoPreference;
        }
        catch
        {
            return ColorContrastPreference.NoPreference;
        }
    }
}
