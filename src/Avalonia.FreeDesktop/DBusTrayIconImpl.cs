using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Platform;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    internal class DBusTrayIconImpl : ITrayIconImpl
    {
        private static int s_trayIconInstanceId;
        public static readonly (int, int, byte[]) EmptyPixmap = (1, 1, new byte[] { 255, 0, 0, 0 });

        private readonly ObjectPath _dbusMenuPath;
        private readonly Connection? _connection;
        private readonly OrgFreedesktopDBus? _dBus;

        private IDisposable? _serviceWatchDisposable;
        private StatusNotifierItemDbusObj? _statusNotifierItemDbusObj;
        private OrgKdeStatusNotifierWatcher? _statusNotifierWatcher;
        private (int, int, byte[]) _icon;

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
            _connection = DBusHelper.TryCreateNewConnection();

            if (_connection is null)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Unable to get a dbus connection for system tray icons.");

                return;
            }

            IsActive = true;

            _dBus = new OrgFreedesktopDBus(_connection, "org.freedesktop.DBus", "/org/freedesktop/DBus");
            _dbusMenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;

            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(_dbusMenuPath, _connection);

            WatchAsync();
        }

        private async void WatchAsync()
        {
            try
            {
                _serviceWatchDisposable = await _dBus!.WatchNameOwnerChangedAsync((_, x) => OnNameChange(x.Item1, x.Item3));
                var nameOwner = await _dBus.GetNameOwnerAsync("org.kde.StatusNotifierWatcher");
                OnNameChange("org.kde.StatusNotifierWatcher", nameOwner);
            }
            catch
            {
                _serviceWatchDisposable = null;
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Interface 'org.kde.StatusNotifierWatcher' is unavailable.");
            }
        }

        private void OnNameChange(string name, string? newOwner)
        {
            if (_isDisposed || _connection is null || name != "org.kde.StatusNotifierWatcher")
                return;

            if (!_serviceConnected & newOwner is not null)
            {
                _serviceConnected = true;
                _statusNotifierWatcher = new OrgKdeStatusNotifierWatcher(_connection, "org.kde.StatusNotifierWatcher", "/StatusNotifierWatcher");

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

            _sysTrayServiceName = FormattableString.Invariant($"org.kde.StatusNotifierItem-{pid}-{tid}");
            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(_connection, _dbusMenuPath);

            _connection.AddMethodHandler(_statusNotifierItemDbusObj);
            await _dBus!.RequestNameAsync(_sysTrayServiceName, 0);
            await _statusNotifierWatcher.RegisterStatusNotifierItemAsync(_sysTrayServiceName);

            _statusNotifierItemDbusObj.SetTitleAndTooltip(_tooltipText);
            _statusNotifierItemDbusObj.SetIcon(_icon);
            _statusNotifierItemDbusObj.ActivationDelegate += OnClicked;
        }

        private void DestroyTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierItemDbusObj is null || _sysTrayServiceName is null)
                return;

            _dBus!.ReleaseNameAsync(_sysTrayServiceName);
        }

        public void Dispose()
        {
            IsActive = false;
            _isDisposed = true;
            DestroyTrayIcon();
            _serviceWatchDisposable?.Dispose();
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
    internal class StatusNotifierItemDbusObj : OrgKdeStatusNotifierItem
    {
        public StatusNotifierItemDbusObj(Connection connection, ObjectPath dbusMenuPath)
        {
            Connection = connection;
            BackingProperties.Menu = dbusMenuPath;
            BackingProperties.Category = string.Empty;
            BackingProperties.Status = string.Empty;
            BackingProperties.Id = string.Empty;
            BackingProperties.Title = string.Empty;
            BackingProperties.IconPixmap = Array.Empty<(int, int, byte[])>();
            BackingProperties.AttentionIconName = string.Empty;
            BackingProperties.AttentionIconPixmap = Array.Empty<(int, int, byte[])>();
            BackingProperties.AttentionMovieName = string.Empty;
            BackingProperties.OverlayIconName = string.Empty;
            BackingProperties.OverlayIconPixmap = Array.Empty<(int, int, byte[])>();
            BackingProperties.ToolTip = (string.Empty, Array.Empty<(int, int, byte[])>(), string.Empty, string.Empty);
            InvalidateAll();
        }

        protected override Connection Connection { get; }

        public override string Path => "/StatusNotifierItem";

        public event Action? ActivationDelegate;

        protected override ValueTask OnContextMenuAsync(int x, int y) => new();

        protected override ValueTask OnActivateAsync(int x, int y)
        {
            ActivationDelegate?.Invoke();
            return new ValueTask();
        }

        protected override ValueTask OnSecondaryActivateAsync(int x, int y) => new();

        protected override ValueTask OnScrollAsync(int delta, string orientation) => new();

        public void InvalidateAll()
        {
            EmitNewTitle();
            EmitNewIcon();
            EmitNewAttentionIcon();
            EmitNewOverlayIcon();
            EmitNewToolTip();
            EmitNewStatus(BackingProperties.Status);
        }

        public void SetIcon((int, int, byte[]) dbusPixmap)
        {
            BackingProperties.IconPixmap = new[] { dbusPixmap };
            InvalidateAll();
        }

        public void SetTitleAndTooltip(string? text)
        {
            if (text is null)
                return;

            BackingProperties.Id = text;
            BackingProperties.Category = "ApplicationStatus";
            BackingProperties.Status = text;
            BackingProperties.Title = text;
            InvalidateAll();
        }
    }
}
