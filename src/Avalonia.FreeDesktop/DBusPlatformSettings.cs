using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using Tmds.DBus.Protocol;
using Avalonia.FreeDesktop.DBus;

namespace Avalonia.FreeDesktop
{
    internal class DBusPlatformSettings : DefaultPlatformSettings
    {
        private readonly Settings? _settings;

        private PlatformColorValues _colorValues;
        private PlatformThemeVariant? _themeVariant;
        private Color? _accentColor;

        public DBusPlatformSettings()
        {
            _colorValues = base.GetColorValues();

            if (DBusHelper.DefaultConnection is not { } conn)
                return;

            _settings = new Settings(conn, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            _ = _settings.WatchSettingChangedAsync(SettingsChangedHandler);
            _ = TryGetInitialValuesAsync();
        }

        public override PlatformColorValues GetColorValues() => _colorValues;

        private async Task TryGetInitialValuesAsync()
        {
            if (_settings is { } settings)
            {
                _themeVariant = await TryGetThemeVariantAsync(settings);
                _accentColor = await TryGetAccentColorAsync(settings);
                UpdateColorValues();
            }
        }

        private static async Task<PlatformThemeVariant?> TryGetThemeVariantAsync(Settings settings)
        {
            try
            {
                var version = await settings.GetVersionAsync();
                VariantValue value;
                if (version >= 2)
                    value = await settings.ReadOneAsync("org.freedesktop.appearance", "color-scheme");
                else
                    // Unpack nested Variant
                    value = (await settings.ReadAsync("org.freedesktop.appearance", "color-scheme")).GetVariantValue();
                return ToColorScheme(value.GetUInt32());
            }
            catch (DBusExceptionBase)
            {
                return null;
            }
        }

        private static async Task<Color?> TryGetAccentColorAsync(Settings settings)
        {
            try
            {
                var version = await settings.GetVersionAsync();
                VariantValue value;
                if (version >= 2)
                    value = await settings.ReadOneAsync("org.freedesktop.appearance", "accent-color");
                else
                    value = await settings.ReadAsync("org.freedesktop.appearance", "accent-color");
                return ToAccentColor(value);
            }
            catch (DBusExceptionBase)
            {
                return null;
            }
        }

        private void UpdateColorValues()
        {
            var oldColorValues = _colorValues;
            var colorValues = BuildPlatformColorValues(_themeVariant, _accentColor);

            if (oldColorValues != colorValues)
            {
                _colorValues = colorValues;
                OnColorValuesChanged(colorValues);
            }
        }

        private void SettingsChangedHandler((string Namespace, string Key, VariantValue Value) tuple)
        {
            switch (tuple)
            {
                case ("org.freedesktop.appearance", "color-scheme", var colorScheme):
                    _themeVariant = ToColorScheme(colorScheme.GetUInt32());
                    UpdateColorValues();
                    break;
                case ("org.freedesktop.appearance", "accent-color", var accentColor):
                    _accentColor = ToAccentColor(accentColor);
                    UpdateColorValues();
                    break;
            }
        }

        private static PlatformColorValues BuildPlatformColorValues(
            PlatformThemeVariant? nullableThemeVariant,
            Color? nullableAccentColor)
        {
            return (nullableThemeVariant, nullableAccentColor) switch
            {
                ({ } themeVariant, { } accentColor)
                    => new PlatformColorValues { ThemeVariant = themeVariant, AccentColor1 = accentColor },
                ({ } themeVariant, null)
                    => new PlatformColorValues { ThemeVariant = themeVariant },
                (null, { } accentColor)
                    => new PlatformColorValues { AccentColor1 = accentColor },
                (null, null)
                    => new PlatformColorValues { ThemeVariant = PlatformThemeVariant.Light }
            };
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
