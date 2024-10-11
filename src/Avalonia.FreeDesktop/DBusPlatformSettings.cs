using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    internal class DBusPlatformSettings : DefaultPlatformSettings
    {
        private readonly OrgFreedesktopPortalSettings? _settings;

        private PlatformColorValues? _lastColorValues;
        private PlatformThemeVariant? _themeVariant;
        private Color? _accentColor;

        public DBusPlatformSettings()
        {
            if (DBusHelper.DefaultConnection is not { } conn)
                return;

            _settings = new OrgFreedesktopPortalSettings(conn, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            _ = _settings.WatchSettingChangedAsync(SettingsChangedHandler);
            _ = TryGetInitialValuesAsync();
        }

        public override PlatformColorValues GetColorValues() => _lastColorValues ?? base.GetColorValues();

        private async Task TryGetInitialValuesAsync()
        {
            _themeVariant = await TryGetThemeVariantAsync();
            _accentColor = await TryGetAccentColorAsync();
            _lastColorValues = BuildPlatformColorValues();
            if (_lastColorValues is not null)
                Dispatcher.UIThread.Post(() => OnColorValuesChanged(_lastColorValues));
        }

        private async Task<PlatformThemeVariant?> TryGetThemeVariantAsync()
        {
            try
            {
                var version = await _settings!.GetVersionPropertyAsync();
                VariantValue value;
                if (version >= 2)
                    value = await _settings!.ReadOneAsync("org.freedesktop.appearance", "color-scheme");
                else
                    // Variants-in-Variants are automatically collapsed by Tmds.DBus.Protocol, so need to do so here as normally necessary
                    value = await _settings!.ReadAsync("org.freedesktop.appearance", "color-scheme");
                return ToColorScheme(value.GetUInt32());
            }
            catch (DBusException)
            {
                return null;
            }
        }

        private async Task<Color?> TryGetAccentColorAsync()
        {
            try
            {
                var version = await _settings!.GetVersionPropertyAsync();
                VariantValue value;
                if (version >= 2)
                    value = await _settings!.ReadOneAsync("org.freedesktop.appearance", "accent-color");
                else
                    value = await _settings!.ReadAsync("org.freedesktop.appearance", "accent-color");
                return ToAccentColor(value);
            }
            catch (DBusException)
            {
                return null;
            }
        }

        private void SettingsChangedHandler(Exception? exception, (string Namespace, string Key, VariantValue Value) tuple)
        {
            if (exception is not null)
                return;

            switch (tuple)
            {
                case ("org.freedesktop.appearance", "color-scheme", var colorScheme):
                    _themeVariant = ToColorScheme(colorScheme.GetUInt32());
                    _lastColorValues = BuildPlatformColorValues();
                    OnColorValuesChanged(_lastColorValues!);
                    break;
                case ("org.freedesktop.appearance", "accent-color", var accentColor):
                    _accentColor = ToAccentColor(accentColor);
                    _lastColorValues = BuildPlatformColorValues();
                    OnColorValuesChanged(_lastColorValues!);
                    break;
            }
        }

        private PlatformColorValues? BuildPlatformColorValues()
        {
            if (_themeVariant is { } themeVariant && _accentColor is { } accentColor)
                return new PlatformColorValues { ThemeVariant = themeVariant, AccentColor1 = accentColor };
            if (_themeVariant is { } themeVariant1)
                return new PlatformColorValues { ThemeVariant = themeVariant1 };
            if (_accentColor is { } accentColor1)
                return new PlatformColorValues { AccentColor1 = accentColor1 };
            return null;
        }

        private static PlatformThemeVariant ToColorScheme(uint value)
        {
            /*
            0: No preference
            1: Prefer dark appearance
            2: Prefer light appearance
            */
            var isDark = value == 1;
            return isDark ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light;
        }

        private static Color? ToAccentColor(VariantValue value)
        {
            /*
            Indicates the system's preferred accent color as a tuple of RGB values
            in the sRGB color space, in the range [0,1].
            Out-of-range RGB values should be treated as an unset accent color.
             */
            var r = value.GetItem(0).GetDouble();
            var g = value.GetItem(1).GetDouble();
            var b = value.GetItem(2).GetDouble();
            if (r is < 0 or > 1 || g is < 0 or > 1 || b is < 0 or > 1)
                return null;
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
