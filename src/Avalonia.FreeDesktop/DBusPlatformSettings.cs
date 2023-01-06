using System;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.FreeDesktop;

internal class DBusPlatformSettings : DefaultPlatformSettings
{
    private readonly IDBusSettings? _settings;
    private PlatformColorValues? _lastColorValues;

    public DBusPlatformSettings()
    {
        _settings = DBusHelper.TryInitialize()?
            .CreateProxy<IDBusSettings>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");

        if (_settings is not null)
        {
            _ = _settings.WatchSettingChangedAsync(SettingsChangedHandler);

            _ = TryGetInitialValue();
        }
    }

    public override PlatformColorValues GetColorValues()
    {
        return _lastColorValues ?? base.GetColorValues();
    }

    private async Task TryGetInitialValue()
    {
        var colorSchemeTask = _settings!.ReadAsync("org.freedesktop.appearance", "color-scheme");
        if (colorSchemeTask.Status == TaskStatus.RanToCompletion)
        {
            _lastColorValues = GetColorValuesFromSetting(colorSchemeTask.Result);
        }
        else
        {
            try
            {
                var value = await colorSchemeTask;
                _lastColorValues = GetColorValuesFromSetting(value);
                OnColorValuesChanged(_lastColorValues.Value);
            }
            catch (Exception ex)
            {
                _lastColorValues = base.GetColorValues();
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, "Unable to get setting value", ex);
            }
        }
    }
    
    private void SettingsChangedHandler((string @namespace, string key, object value) tuple)
    {
        if (tuple.@namespace == "org.freedesktop.appearance"
            && tuple.key == "color-scheme")
        {
            /*
            <member>0: No preference</member>
            <member>1: Prefer dark appearance</member>
            <member>2: Prefer light appearance</member>
            */
            _lastColorValues = GetColorValuesFromSetting(tuple.value);
            OnColorValuesChanged(_lastColorValues.Value);
        }
    }
    
    private static PlatformColorValues GetColorValuesFromSetting(object value)
    {
        var isDark = value?.ToString() == "1";
        return new PlatformColorValues(isDark ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light);
    }
}
