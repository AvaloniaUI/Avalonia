using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
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
            if (DBusHelper.Connection is null)
                return;

            _settings = new OrgFreedesktopPortalSettings(DBusHelper.Connection, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
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
                OnColorValuesChanged(_lastColorValues);
        }

        private async Task<PlatformThemeVariant?> TryGetThemeVariantAsync()
        {
            try
            {
                var version = await _settings!.GetVersionPropertyAsync();
                DBusVariantItem value;
                if (version >= 2)
                    value = await _settings!.ReadOneAsync("org.freedesktop.appearance", "color-scheme");
                else
                    value = (DBusVariantItem)(await _settings!.ReadAsync("org.freedesktop.appearance", "color-scheme")).Value;
                if (value.Value is DBusUInt32Item dBusUInt32Item)
                    return ToColorScheme(dBusUInt32Item.Value);
                return null;
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
                DBusVariantItem value;
                if (version >= 2)
                    value = await _settings!.ReadOneAsync("org.freedesktop.appearance", "accent-color");
                else
                    value = (DBusVariantItem)(await _settings!.ReadAsync("org.freedesktop.appearance", "accent-color")).Value;
                if (value.Value is DBusStructItem dBusStructItem)
                    return ToAccentColor(dBusStructItem);
                return null;
            }
            catch (DBusException)
            {
                return null;
            }
        }

        private void SettingsChangedHandler(Exception? exception, (string @namespace, string key, DBusVariantItem value) valueTuple)
        {
            if (exception is not null)
                return;

            switch (valueTuple)
            {
                case ("org.freedesktop.appearance", "color-scheme", { } colorScheme):
                    _themeVariant = ToColorScheme((colorScheme.Value as DBusUInt32Item)!.Value);
                    _lastColorValues = BuildPlatformColorValues();
                    OnColorValuesChanged(_lastColorValues!);
                    break;
                case ("org.freedesktop.appearance", "accent-color", { } accentColor):
                    _accentColor = ToAccentColor((accentColor.Value as DBusStructItem)!);
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

        private static Color? ToAccentColor(DBusStructItem value)
        {
            /*
            Indicates the system's preferred accent color as a tuple of RGB values
            in the sRGB color space, in the range [0,1].
            Out-of-range RGB values should be treated as an unset accent color.
             */
            var r = (value[0] as DBusDoubleItem)!.Value;
            var g = (value[1] as DBusDoubleItem)!.Value;
            var b = (value[2] as DBusDoubleItem)!.Value;
            if (r is < 0 or > 1 || g is < 0 or > 1 || b is < 0 or > 1)
                return null;
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
