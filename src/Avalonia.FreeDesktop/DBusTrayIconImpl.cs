using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.DBus;
using Avalonia.FreeDesktop.DBusXml;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.FreeDesktop
{
    internal class DBusTrayIconImpl : ITrayIconImpl
    {
        private static int s_trayIconInstanceId;
        public static readonly (int, int, byte[]) EmptyPixmap = (1, 1, [255, 0, 0, 0]);

        private readonly DBusConnection? _connection;
        private readonly OrgFreedesktopDBusProxy? _dBus;

        private IDisposable? _serviceWatchDisposable;
        private IDisposable? _registration;
        private readonly StatusNotifierItemDbusObj? _statusNotifierItemDbusObj;
        private OrgKdeStatusNotifierWatcherProxy? _statusNotifierWatcher;
        private (int, int, byte[]) _icon;

        private readonly AvaloniaSynchronizationContext _synchronizationContext = new(DispatcherPriority.Input);
        private string? _sysTrayServiceName;
        private string? _tooltipText;
        private bool _isDisposed;
        private bool _serviceConnected;
        private bool _isVisible = true;

        public bool IsActive { get; private set; }
        public INativeMenuExporter? MenuExporter { get; }
        public Action? OnClicked { get; set; }
        public Func<IWindowIconImpl?, uint[]>? IconConverterDelegate { get; set; }

        public DBusTrayIconImpl()
        {
            using var restoreContext = AvaloniaSynchronizationContext.Ensure(DispatcherPriority.Input);
            _connection = DBusHelper.TryCreateNewConnection();

            if (_connection is null)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Unable to get a dbus connection for system tray icons.");

                return;
            }

            IsActive = true;

            _dBus = new OrgFreedesktopDBusProxy(_connection, "org.freedesktop.DBus", new DBusObjectPath("/org/freedesktop/DBus"));
            var dbusMenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;

            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(dbusMenuPath, _connection);

            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(_connection, dbusMenuPath);
            _statusNotifierItemDbusObj.ActivationDelegate += () => OnClicked?.Invoke();

            _ = RegisterAndWatchAsync();
        }

        private async Task RegisterAndWatchAsync()
        {
            try
            {
                _registration = await _connection!.RegisterObjects(
                    (DBusObjectPath)"/StatusNotifierItem",
                    new object[] { _statusNotifierItemDbusObj! },
                    _synchronizationContext);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Failed to register StatusNotifierItem handler: {Exception}", e);
                return;
            }

            await WatchAsync();
        }

        private async Task WatchAsync()
        {
            try
            {
                _serviceWatchDisposable = await _dBus!.WatchNameOwnerChangedAsync(
                    (name, _, newOwner) => OnNameChange(name, newOwner));
                var nameOwner = await _dBus.GetNameOwnerAsync("org.kde.StatusNotifierWatcher");
                OnNameChange("org.kde.StatusNotifierWatcher", nameOwner);
            }
            catch (Exception e)
            {
                _serviceWatchDisposable = null;
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Interface 'org.kde.StatusNotifierWatcher' is unavailable.\n{Exception}", e);
            }
        }

        private void OnNameChange(string name, string? newOwner)
        {
            if (_isDisposed || _connection is null || name != "org.kde.StatusNotifierWatcher")
                return;

            if (!_serviceConnected && newOwner is not null)
            {
                _serviceConnected = true;
                _statusNotifierWatcher = new OrgKdeStatusNotifierWatcherProxy(_connection, "org.kde.StatusNotifierWatcher", new DBusObjectPath("/StatusNotifierWatcher"));

                DestroyTrayIcon();

                if (_isVisible)
                    CreateTrayIcon();
            }
            else if (_serviceConnected & newOwner is null)
            {
                DestroyTrayIcon();
                _serviceConnected = false;
            }
        }

        private async void CreateTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierWatcher is null)
                return;

#if NET5_0_OR_GREATER
            var pid = Environment.ProcessId;
#else
            var pid = Process.GetCurrentProcess().Id;
#endif
            var tid = s_trayIconInstanceId++;

            // Re-register the handler object if needed
            _registration?.Dispose();
            try
            {
                _registration = await _connection.RegisterObjects(
                    (DBusObjectPath)"/StatusNotifierItem",
                    new object[] { _statusNotifierItemDbusObj! },
                    _synchronizationContext);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Failed to register StatusNotifierItem handler: {Exception}", e);
                return;
            }

            _sysTrayServiceName = FormattableString.Invariant($"org.kde.StatusNotifierItem-{pid}-{tid}");
            await _connection.RequestNameAsync(_sysTrayServiceName);
            await _statusNotifierWatcher.RegisterStatusNotifierItemAsync(_sysTrayServiceName);

            _statusNotifierItemDbusObj!.SetTitleAndTooltip(_tooltipText);
            _statusNotifierItemDbusObj.SetIcon(_icon);
        }

        private void DestroyTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierItemDbusObj is null || _sysTrayServiceName is null)
                return;

            _connection.ReleaseNameAsync(_sysTrayServiceName);
            _registration?.Dispose();
            _registration = null;
        }

        public void Dispose()
        {
            IsActive = false;
            DestroyTrayIcon();
            (MenuExporter as IDisposable)?.Dispose();
            _serviceWatchDisposable?.Dispose();
            _isDisposed = true;
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (_isDisposed || IconConverterDelegate is null)
                return;

            if (icon is null)
            {
                _statusNotifierItemDbusObj?.SetIcon(EmptyPixmap);
                return;
            }

            var x11iconData = IconConverterDelegate(icon);

            if (x11iconData.Length == 0)
                return;

            var w = (int)x11iconData[0];
            var h = (int)x11iconData[1];

            var pixLength = w * h;
            var pixByteArrayCounter = 0;
            var pixByteArray = new byte[w * h * 4];

            for (var i = 0; i < pixLength; i++)
            {
                var rawPixel = x11iconData[i + 2];
                pixByteArray[pixByteArrayCounter++] = (byte)((rawPixel & 0xFF000000) >> 24);
                pixByteArray[pixByteArrayCounter++] = (byte)((rawPixel & 0xFF0000) >> 16);
                pixByteArray[pixByteArrayCounter++] = (byte)((rawPixel & 0xFF00) >> 8);
                pixByteArray[pixByteArrayCounter++] = (byte)(rawPixel & 0xFF);
            }

            _icon = (w, h, pixByteArray);
            _statusNotifierItemDbusObj?.SetIcon(_icon);
        }

        public void SetIsVisible(bool visible)
        {
            if (_isDisposed || !_serviceConnected)
            {
                _isVisible = visible;
                return;
            }

            switch (visible)
            {
                case true when !_isVisible:
                    DestroyTrayIcon();
                    CreateTrayIcon();
                    break;
                case false when _isVisible:
                    DestroyTrayIcon();
                    break;
            }

            _isVisible = visible;
        }

        public void SetToolTipText(string? text)
        {
            if (_isDisposed || text is null)
                return;
            _tooltipText = text;
            _statusNotifierItemDbusObj?.SetTitleAndTooltip(_tooltipText);
        }
    }

    /// <summary>
    /// DBus Object used for setting system tray icons.
    /// </summary>
    /// <remarks>
    /// Useful guide: https://web.archive.org/web/20210818173850/https://www.notmart.org/misc/statusnotifieritem/statusnotifieritem.html
    /// </remarks>
    internal class StatusNotifierItemDbusObj : IOrgKdeStatusNotifierItem
    {
        private const string InterfaceName = "org.kde.StatusNotifierItem";
        private readonly DBusConnection _connection;

        public StatusNotifierItemDbusObj(DBusConnection connection, string dbusMenuPath)
        {
            _connection = connection;
            Menu = (DBusObjectPath)dbusMenuPath;
        }

        public event Action? ActivationDelegate;

        // IOrgKdeStatusNotifierItem properties
        public string Category { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int WindowId { get; } = 0;
        public string IconThemePath { get; } = string.Empty;
        public DBusObjectPath Menu { get; }
        public bool ItemIsMenu { get; } = false;
        public string IconName { get; } = string.Empty;
        public List<IconPixmap> IconPixmap { get; set; } = [];
        public string OverlayIconName { get; } = string.Empty;
        public List<IconPixmap> OverlayIconPixmap { get; } = [];
        public string AttentionIconName { get; } = string.Empty;
        public List<IconPixmap> AttentionIconPixmap { get; } = [];
        public string AttentionMovieName { get; } = string.Empty;
        public ToolTip ToolTip { get; set; } = new(string.Empty, [], string.Empty, string.Empty);

        // IOrgKdeStatusNotifierItem methods
        public ValueTask ContextMenuAsync(int x, int y) => new();

        public ValueTask ActivateAsync(int x, int y)
        {
            ActivationDelegate?.Invoke();
            return new ValueTask();
        }

        public ValueTask SecondaryActivateAsync(int x, int y) => new();

        public ValueTask ScrollAsync(int delta, string orientation) => new();

        // Signal emission helpers
        private void EmitSignal(string signalName, params object[] body)
        {
            var message = DBusMessage.CreateSignal(
                (DBusObjectPath)"/StatusNotifierItem",
                InterfaceName,
                signalName,
                body);
            _ = _connection.SendMessageAsync(message);
        }

        public void InvalidateAll()
        {
            EmitSignal("NewTitle");
            EmitSignal("NewIcon");
            EmitSignal("NewAttentionIcon");
            EmitSignal("NewOverlayIcon");
            EmitSignal("NewToolTip");
            EmitSignal("NewStatus", Status);
        }

        public void SetIcon((int, int, byte[]) dbusPixmap)
        {
            IconPixmap = [new IconPixmap(dbusPixmap.Item1, dbusPixmap.Item2, new List<byte>(dbusPixmap.Item3))];
            InvalidateAll();
        }

        public void SetTitleAndTooltip(string? text)
        {
            if (text is null)
                return;

            Id = text;
            Category = "ApplicationStatus";
            Status = text;
            Title = text;
            InvalidateAll();
        }
    }
}
