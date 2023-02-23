using System;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    internal class DBusPlatformSettings : DefaultPlatformSettings
    {
        private readonly OrgFreedesktopPortalSettings? _settings;
        private PlatformColorValues? _lastColorValues;

        public DBusPlatformSettings()
        {
            if (DBusHelper.Connection is null)
                return;

            _settings = new OrgFreedesktopPortalSettings(DBusHelper.Connection, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            _ = _settings.WatchSettingChangedAsync(SettingsChangedHandler);
            _ = TryGetInitialValueAsync();
        }

        public override PlatformColorValues GetColorValues()
        {
            return _lastColorValues ?? base.GetColorValues();
        }

        private async Task TryGetInitialValueAsync()
        {
            try
            {
                var value = await _settings!.ReadAsync("org.freedesktop.appearance", "color-scheme");
                _lastColorValues = GetColorValuesFromSetting(value);
                OnColorValuesChanged(_lastColorValues);
            }
            catch (Exception ex)
            {
                _lastColorValues = base.GetColorValues();
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, "Unable to get setting value", ex);
            }
        }

        private void SettingsChangedHandler(Exception? exception, (string @namespace, string key, DBusVariantItem value) valueTuple)
        {
            if (exception is not null)
                return;

            if (valueTuple is ("org.freedesktop.appearance", "color-scheme", { } value))
            {
                /*
                <member>0: No preference</member>
                <member>1: Prefer dark appearance</member>
                <member>2: Prefer light appearance</member>
                */
                _lastColorValues = GetColorValuesFromSetting(value);
                OnColorValuesChanged(_lastColorValues);
            }
        }

        private static PlatformColorValues GetColorValuesFromSetting(DBusVariantItem value)
        {
            var isDark = ((value.Value as DBusVariantItem)!.Value as DBusUInt32Item)!.Value == 1;
            return new PlatformColorValues
            {
                ThemeVariant = isDark ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light
            };
        }
    }
}
