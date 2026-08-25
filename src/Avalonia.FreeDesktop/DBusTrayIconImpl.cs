using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using Avalonia.FreeDesktop.DBus;

namespace Avalonia.FreeDesktop
{
    internal class DBusTrayIconImpl : ITrayIconImpl
    {
        private static int s_trayIconInstanceId;
        public static readonly (int, int, byte[]) EmptyPixmap = (1, 1, [255, 0, 0, 0]);

        private readonly DBusConnection? _connection;
        private CancellationTokenSource? _watchCts;
        private readonly StatusNotifierItemDbusObj? _statusNotifierItemDbusObj;
        private StatusNotifierWatcher? _statusNotifierWatcher;
        private (int, int, byte[]) _icon;

        private string? _sysTrayServiceName;
        // Non-null from the moment the name is asked for. Tracking the request instead of a bool keeps
        // a refused acquisition from being mistaken for ownership, and lets a hide that races the
        // request wait for it before releasing.
        private Task? _sysTrayServiceNameRequest;
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

            var dbusMenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;

            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(dbusMenuPath, _connection);

            // Note: the object is only exported from CreateTrayIcon, once the name is owned.
            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(_connection, dbusMenuPath);
            _statusNotifierItemDbusObj.ActivationDelegate += () => OnClicked?.Invoke();

            WatchAsync();
        }

        private async void WatchAsync()
        {
            try
            {
                _watchCts = new CancellationTokenSource();
                using var watcher = await _connection!.WatchNameOwnerAsync("org.kde.StatusNotifierWatcher");
                var owner = watcher.GetCurrentOwner();
                OnOwnerChanged(owner);
                while (!_watchCts.IsCancellationRequested)
                {
                    if (owner is not null)
                    {
                        var ct = watcher.GetOwnerChangedCancellationToken(owner);
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _watchCts.Token);
                        try
                        {
                            await Task.Delay(Timeout.Infinite, linked.Token);
                        }
                        catch (OperationCanceledException) when (!_watchCts.IsCancellationRequested)
                        { }
                    }
                    else
                    {
                        try
                        {
                            await watcher.WaitForOwnerAsync(_watchCts.Token);
                        }
                        catch (OperationCanceledException) when (!_watchCts.IsCancellationRequested)
                        { }
                    }
                    owner = watcher.GetCurrentOwner();
                    OnOwnerChanged(owner);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled by Dispose. Can't be filtered on _isDisposed: the continuation
                // is posted to the dispatcher and runs after Dispose has completed.
            }
            catch (Exception e) when (!_isDisposed)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Interface 'org.kde.StatusNotifierWatcher' is unavailable.\n{Exception}", e);
            }
        }

        private void OnOwnerChanged(string? newOwner)
        {
            if (_isDisposed || _connection is null)
                return;

            if (!_serviceConnected && newOwner is not null)
            {
                _serviceConnected = true;
                _statusNotifierWatcher = new StatusNotifierWatcher(_connection, "org.kde.StatusNotifierWatcher", "/StatusNotifierWatcher");

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
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierItemDbusObj is null || _statusNotifierWatcher is null)
                return;

            try
            {
                // Reused for the lifetime of the icon: a new instance id looks like a new item to the host.
                if (_sysTrayServiceName is null)
                {
#if NET5_0_OR_GREATER
                    var pid = Environment.ProcessId;
#else
                    var pid = Process.GetCurrentProcess().Id;
#endif
                    var tid = s_trayIconInstanceId++;
                    _sysTrayServiceName = FormattableString.Invariant($"org.kde.StatusNotifierItem-{pid}-{tid}");
                }

                _sysTrayServiceNameRequest ??= _connection.RequestNameAsync(_sysTrayServiceName);
                await _sysTrayServiceNameRequest;

                // Exported only while the name is owned: a host scanning the bus would otherwise find the
                // object under the unique connection name and register a second, duplicate item.
                _connection.AddMethodHandler(_statusNotifierItemDbusObj);

                await _statusNotifierWatcher.RegisterStatusNotifierItemAsync(_sysTrayServiceName);

                _statusNotifierItemDbusObj.SetTitleAndTooltip(_tooltipText);
                _statusNotifierItemDbusObj.SetIcon(_icon);
            }
            catch (Exception e)
            {
                // A refused name must not stick around as if it were owned: drop the request so the
                // next attempt asks again. A failure after that point leaves ownership untouched.
                if (_sysTrayServiceNameRequest is { Status: not TaskStatus.RanToCompletion })
                    _sysTrayServiceNameRequest = null;

                if (!_isDisposed)
                    Logger.TryGet(LogEventLevel.Error, "DBUS")
                        ?.Log(this, "Unable to register the system tray icon.\n{Exception}", e);
            }
        }

        private void DestroyTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierItemDbusObj is null || _sysTrayServiceName is null)
                return;

            _connection.RemoveMethodHandler(_statusNotifierItemDbusObj.Path);
        }

        /// <summary>
        /// Releasing the name is what makes a host drop the item, unexporting the object isn't observable.
        /// Call after <see cref="DestroyTrayIcon"/>, and not when the watcher merely goes away: the object
        /// must never be exported while the name is unowned.
        /// </summary>
        private void ReleaseTrayServiceName()
        {
            if (_connection is null || _sysTrayServiceName is null || _sysTrayServiceNameRequest is not { } request)
                return;

            _sysTrayServiceNameRequest = null;
            ReleaseTrayServiceName(request, _connection, _sysTrayServiceName);
        }

        private async void ReleaseTrayServiceName(Task request, DBusConnection connection, string name)
        {
            try
            {
                // Awaiting the request first: releasing a name that is still being acquired would
                // leave it owned. If the acquisition failed there is nothing to release.
                await request;
                await connection.ReleaseNameAsync(name);
            }
            catch (Exception e)
            {
                if (!_isDisposed)
                    Logger.TryGet(LogEventLevel.Error, "DBUS")
                        ?.Log(this, "Unable to release the system tray icon name.\n{Exception}", e);
            }
        }

        public void Dispose()
        {
            IsActive = false;
            DestroyTrayIcon();
            ReleaseTrayServiceName();
            (MenuExporter as IDisposable)?.Dispose();
            _watchCts?.Cancel();
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
                    ReleaseTrayServiceName();
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
    internal class StatusNotifierItemDbusObj : DBusHandler, IStatusNotifierItemHandler, IStatusNotifierItemProperties
    {
        // The item is active, is more important that the item will be shown in some way to the user.
        private const string StatusActive = "Active";
        private string _category = "";
        private string _id = "";
        private string _title = "";
        private ObjectPath _menu;
        private (int, int, byte[])[] _iconPixmap = [];

        public StatusNotifierItemDbusObj(DBusConnection connection, ObjectPath dbusMenuPath)
            : base(connection, "/StatusNotifierItem", handlesChildPaths: false)
        {
            _menu = dbusMenuPath;
        }

        public event Action? ActivationDelegate;

        string IStatusNotifierItemProperties.Category => _category;
        string IStatusNotifierItemProperties.Id => _id;
        string IStatusNotifierItemProperties.Title => _title;
        string IStatusNotifierItemProperties.Status => StatusActive;
        int IStatusNotifierItemProperties.WindowId => 0;
        string IStatusNotifierItemProperties.IconThemePath => "";
        ObjectPath IStatusNotifierItemProperties.Menu => _menu;
        bool IStatusNotifierItemProperties.ItemIsMenu => false;
        string IStatusNotifierItemProperties.IconName => "";
        (int, int, byte[])[] IStatusNotifierItemProperties.IconPixmap => _iconPixmap;
        string IStatusNotifierItemProperties.OverlayIconName => "";
        (int, int, byte[])[] IStatusNotifierItemProperties.OverlayIconPixmap => [];
        string IStatusNotifierItemProperties.AttentionIconName => "";
        (int, int, byte[])[] IStatusNotifierItemProperties.AttentionIconPixmap => [];
        string IStatusNotifierItemProperties.AttentionMovieName => "";
        (string, (int, int, byte[])[], string, string) IStatusNotifierItemProperties.ToolTip => ("", [], "", "");

        ValueTask IStatusNotifierItemHandler.HandleGetPropertyAsync(IStatusNotifierItemHandler.GetPropertyContext context)
            => context.Handle(this);

        ValueTask IStatusNotifierItemHandler.HandleGetAllPropertiesAsync(IStatusNotifierItemHandler.GetAllPropertiesContext context)
            => context.Handle(this);

        ValueTask IStatusNotifierItemHandler.ContextMenuAsync(int x, int y) => new();

        ValueTask IStatusNotifierItemHandler.ActivateAsync(int x, int y)
        {
            ActivationDelegate?.Invoke();
            return new ValueTask();
        }

        ValueTask IStatusNotifierItemHandler.SecondaryActivateAsync(int x, int y) => new();

        ValueTask IStatusNotifierItemHandler.ScrollAsync(int delta, string orientation) => new();

        public void InvalidateAll()
        {
            Connection.EmitNewTitle(Path);
            Connection.EmitNewIcon(Path);
            Connection.EmitNewAttentionIcon(Path);
            Connection.EmitNewOverlayIcon(Path);
            Connection.EmitNewToolTip(Path);
            Connection.EmitNewStatus(Path, StatusActive);
        }

        public void SetIcon((int, int, byte[]) dbusPixmap)
        {
            _iconPixmap = [dbusPixmap];
            InvalidateAll();
        }

        public void SetTitleAndTooltip(string? text)
        {
            if (text is null)
                return;

            _id = text;
            _category = "ApplicationStatus";
            _title = text;
            InvalidateAll();
        }
    }
}
