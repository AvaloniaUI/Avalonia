using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.DBus;
using Avalonia.FreeDesktop.DBusXml;

namespace Avalonia.FreeDesktop
{
    internal class DBusPlatformSettings : DefaultPlatformSettings
    {
        private readonly OrgFreedesktopPortalSettingsProxy? _settings;

        private PlatformColorValues? _lastColorValues;
        private PlatformThemeVariant? _themeVariant;
        private Color? _accentColor;

        public DBusPlatformSettings()
        {
            if (DBusHelper.DefaultConnection is not { } conn)
                return;

            _settings = new OrgFreedesktopPortalSettingsProxy(conn, "org.freedesktop.portal.Desktop", new DBusObjectPath("/org/freedesktop/portal/desktop"));
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
                DBusVariant value;
                if (version >= 2)
                    value = await _settings!.ReadOneAsync("org.freedesktop.appearance", "color-scheme");
                else
                {
                    // Unpack nested Variant
                    var outer = await _settings!.ReadAsync("org.freedesktop.appearance", "color-scheme");
                    value = (DBusVariant)outer.Value;
                }
                return ToColorScheme((uint)value.Value);
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
                DBusVariant value;
                if (version >= 2)
                    value = await _settings!.ReadOneAsync("org.freedesktop.appearance", "accent-color");
                else
                {
                    // Unpack nested Variant
                    var outer = await _settings!.ReadAsync("org.freedesktop.appearance", "accent-color");
                    value = (DBusVariant)outer.Value;
                }
                return ToAccentColor(value);
            }
            catch (DBusException)
            {
                return null;
            }
        }

        private void SettingsChangedHandler(string ns, string key, DBusVariant value)
        {
            switch ((ns, key))
            {
                case ("org.freedesktop.appearance", "color-scheme"):
                    _themeVariant = ToColorScheme((uint)value.Value);
                    _lastColorValues = BuildPlatformColorValues();
                    OnColorValuesChanged(_lastColorValues!);
                    break;
                case ("org.freedesktop.appearance", "accent-color"):
                    _accentColor = ToAccentColor(value);
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

        private static Color? ToAccentColor(DBusVariant value)
        {
            /*
            Indicates the system's preferred accent color as a tuple of RGB values
            in the sRGB color space, in the range [0,1].
            Out-of-range RGB values should be treated as an unset accent color.
             */
            var rgb = (DBusStruct)value.Value;
            var r = (double)rgb[0];
            var g = (double)rgb[1];
            var b = (double)rgb[2];
            if (r is < 0 or > 1 || g is < 0 or > 1 || b is < 0 or > 1)
                return null;
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
