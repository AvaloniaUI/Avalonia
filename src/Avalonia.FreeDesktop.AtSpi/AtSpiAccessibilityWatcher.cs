using System;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class AtSpiAccessibilityWatcher : IAsyncDisposable
    {
        private DBusConnection? _sessionConnection;
        private IDisposable? _propertiesWatcher;

        public bool IsEnabled { get; private set; }
        public event EventHandler<bool>? IsEnabledChanged;

        public async Task InitAsync()
        {
            try
            {
                _sessionConnection = await DBusConnection.ConnectSessionAsync();
                var proxy = new OrgA11yStatusProxy(
                    _sessionConnection, BusNameA11y, new DBusObjectPath(PathA11y));

                try
                {
                    var props = await proxy.GetAllPropertiesAsync();
                    IsEnabled = props.IsEnabled || props.ScreenReaderEnabled;
                }
                catch
                {
                    IsEnabled = false;
                }

                _propertiesWatcher = await proxy.WatchPropertiesChangedAsync(
                    (changed, _, _) =>
                    {
                        var enabled = changed.IsEnabled || changed.ScreenReaderEnabled;
                        if (enabled == IsEnabled) return;
                        IsEnabled = enabled;
                        IsEnabledChanged?.Invoke(this, enabled);
                    },
                    sender: null,
                    emitOnCapturedContext: true);
            }
            catch
            {
                // D-Bus session bus unavailable or org.a11y.Bus not present.
                // Silently degrade - accessibility remains disabled.
                IsEnabled = false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _propertiesWatcher?.Dispose();
            _propertiesWatcher = null;

            if (_sessionConnection is not null)
            {
                await _sessionConnection.DisposeAsync();
                _sessionConnection = null;
            }
        }
    }
}
