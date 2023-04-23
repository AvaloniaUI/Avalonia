using System;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
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
            _ = TryGetInitialValueAsync();
        }

        public override PlatformColorValues GetColorValues() => _lastColorValues ?? base.GetColorValues();

        private async Task TryGetInitialValueAsync()
        {
            try
            {
                var value = await _settings!.ReadAsync("org.freedesktop.appearance", "color-scheme");
                _themeVariant = ReadAsColorScheme(value);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, "Unable to get org.freedesktop.appearance.color-scheme value", ex);
            }

            try
            {
                var value = await _settings!.ReadAsync("org.kde.kdeglobals.General", "AccentColor");
                _accentColor = ReadAsAccentColor(value);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, "Unable to get org.kde.kdeglobals.General.AccentColor value", ex);
            }

            _lastColorValues = BuildPlatformColorValues();
            if (_lastColorValues is not null)
                OnColorValuesChanged(_lastColorValues);
        }

        private void SettingsChangedHandler(Exception? exception, (string @namespace, string key, DBusVariantItem value) valueTuple)
        {
            if (exception is not null)
                return;

            switch (valueTuple)
            {
                case ("org.freedesktop.appearance", "color-scheme", { } colorScheme):
                    _themeVariant = ReadAsColorScheme(colorScheme);
                    _lastColorValues = BuildPlatformColorValues();
                    OnColorValuesChanged(_lastColorValues!);
                    break;
                case ("org.kde.kdeglobals.General", "AccentColor", { } accentColor):
                    _accentColor = ReadAsAccentColor(accentColor);
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

        private static PlatformThemeVariant ReadAsColorScheme(DBusVariantItem value)
        {
            /*
            <member>0: No preference</member>
            <member>1: Prefer dark appearance</member>
            <member>2: Prefer light appearance</member>
            */
            var isDark = ((value.Value as DBusVariantItem)!.Value as DBusUInt32Item)!.Value == 1;
            return isDark ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light;
        }

        private static Color ReadAsAccentColor(DBusVariantItem value)
        {
            var colorStr = ((value.Value as DBusVariantItem)!.Value as DBusStringItem)!.Value;
            var rgb = colorStr.Split(',');
            return new Color(255, byte.Parse(rgb[0]), byte.Parse(rgb[1]), byte.Parse(rgb[2]));
        }
    }
}
