using System;
using Android.Content;
using Android.Content.Res;
using Android.Provider;
using Android.Views;
using AndroidX.Core.Content;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Color = Avalonia.Media.Color;

namespace Avalonia.Android.Platform;

internal class AndroidPlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues _colorValues;
    private TimeSpan _holdWaitDuration = TimeSpan.FromMilliseconds(300);
    private TimeSpan _doubleTapTime = TimeSpan.FromMilliseconds(500);
    private Size _doubleTapSize = new Size(16,16);
    private Size _tapSize = new Size(10,10);
    private string? _latestLanguage;

    public AndroidPlatformSettings()
    {
        if (global::Android.App.Application.Context is { } context)
        {
            UpdateInputConfigValues(context);
            _colorValues = GetColorValuesFromContext(context);
            _latestLanguage = QueryPreferredApplicationLanguage(context);

            ContextCompat.RegisterReceiver(
                context,
                new ConfigurationChangedReceiver(this),
                new IntentFilter(Intent.ActionConfigurationChanged),
                ContextCompat.ReceiverNotExported);
        }
        else
            _colorValues = base.GetColorValues();
    }

    public override PlatformColorValues GetColorValues()
    {
        return _colorValues;
    }

    public override string PreferredApplicationLanguage => _latestLanguage ?? base.PreferredApplicationLanguage;

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

    private void UpdateColorValues(Context context)
    {
        var oldColorValues = _colorValues;
        var colorValues = GetColorValuesFromContext(context);

        if (oldColorValues != colorValues)
        {
            _colorValues = colorValues;
            OnColorValuesChanged(colorValues);
        }
    }

    private void UpdatePreferredApplicationLanguage(Context context)
    {
        var oldLanguage = _latestLanguage;
        var language = QueryPreferredApplicationLanguage(context);

        if (oldLanguage != language)
        {
            _latestLanguage = language;
            OnPreferredApplicationLanguageChanged();
        }
    }

    private static PlatformColorValues GetColorValuesFromContext(Context context)
    {
        var uiMode = context.Resources?.Configuration?.UiMode & UiMode.NightMask ?? UiMode.NightNo;
        var systemTheme = uiMode == UiMode.NightYes ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light;
        var contrastPreference = IsHighContrast(context);

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            if (context.Resources is { } resources)
            {
                // See https://developer.android.com/reference/android/R.color
                var accent1 = resources.GetColor(17170494, context.Theme); // Resource.Color.SystemAccent1500
                var accent2 = resources.GetColor(17170507, context.Theme); // Resource.Color.SystemAccent2500
                var accent3 = resources.GetColor(17170520, context.Theme); // Resource.Color.SystemAccent3500

                return new PlatformColorValues
                {
                    ThemeVariant = systemTheme,
                    ContrastPreference = contrastPreference,
                    AccentColor1 = new Color(accent1.A, accent1.R, accent1.G, accent1.B),
                    AccentColor2 = new Color(accent2.A, accent2.R, accent2.G, accent2.B),
                    AccentColor3 = new Color(accent3.A, accent3.R, accent3.G, accent3.B),
                };
            }
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            // See https://developer.android.com/reference/android/R.attr
            var array = context.Theme?.ObtainStyledAttributes([16843829]); // Resource.Attribute.ColorAccent
            if (array is not null)
            {
                try
                {
                    var accent = array.GetColor(0, 0);

                    return new PlatformColorValues
                    {
                        ThemeVariant = systemTheme,
                        ContrastPreference = contrastPreference,
                        AccentColor1 = new Color(accent.A, accent.R, accent.G, accent.B)
                    };
                }
                finally
                {
                    array.Recycle();
                }
            }
        }

        return new PlatformColorValues
        {
            ThemeVariant = systemTheme,
            ContrastPreference = contrastPreference
        };
    }

    private void UpdateInputConfigValues(Context context)
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

    private string? QueryPreferredApplicationLanguage(Context? context)
    {
        var locale = context?.Resources?.Configuration?.Locales?.Get(0);
        return locale?.ToLanguageTag() is { Length: > 0 } tag ? tag : null;
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

    private sealed class ConfigurationChangedReceiver(AndroidPlatformSettings settings) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context is null)
                return;

            // The context might still have the old values at this point because they haven't been processed yet.
            // Postpone the update 100ms arbitrarily. Not an ideal solution, but sufficient.
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, Dispatcher.UIThread);

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                settings.UpdateInputConfigValues(context);
                settings.UpdateColorValues(context);
                settings.UpdatePreferredApplicationLanguage(context);
            };

            timer.Start();
        }
    }
}
