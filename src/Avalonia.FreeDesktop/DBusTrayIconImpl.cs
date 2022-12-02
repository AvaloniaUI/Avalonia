#nullable enable

using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Platform;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]

namespace Avalonia.FreeDesktop
{
    internal class DBusTrayIconImpl : ITrayIconImpl
    {
        private static int s_trayIconInstanceId;

        private readonly ObjectPath _dbusMenuPath;
        private readonly Connection? _connection;
        private IDisposable? _serviceWatchDisposable;

        private StatusNotifierItemDbusObj? _statusNotifierItemDbusObj;
        private IStatusNotifierWatcher? _statusNotifierWatcher;
        private DbusPixmap _icon;

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

            _dbusMenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;

            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(_dbusMenuPath, _connection);

            WatchAsync();
        }

        private void InitializeSNWService()
        {
            if (_connection is null || _isDisposed) return;

            try
            {
                _statusNotifierWatcher = _connection.CreateProxy<IStatusNotifierWatcher>(
                    "org.kde.StatusNotifierWatcher",
                    "/StatusNotifierWatcher");
            }
            catch
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this,
                        "org.kde.StatusNotifierWatcher service is not available on this system. Tray Icons will not work without it.");

                return;
            }

            _serviceConnected = true;
        }

        private async void WatchAsync()
        {
            try
            {
                _serviceWatchDisposable =
                    await _connection?.ResolveServiceOwnerAsync("org.kde.StatusNotifierWatcher", OnNameChange)!;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this,
                        $"Unable to hook watcher method on org.kde.StatusNotifierWatcher: {e}");
            }
        }

        private void OnNameChange(ServiceOwnerChangedEventArgs obj)
        {
            if (_isDisposed)
                return;

            if (!_serviceConnected & obj.NewOwner != null)
            {
                _serviceConnected = true;
                InitializeSNWService();

                DestroyTrayIcon();

                if (_isVisible)
                {
                    CreateTrayIcon();
                }
            }
            else if (_serviceConnected & obj.NewOwner is null)
            {
                DestroyTrayIcon();
                _serviceConnected = false;
            }
        }

        private void CreateTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed)
                return;

            var pid = Process.GetCurrentProcess().Id;
            var tid = s_trayIconInstanceId++;

            _sysTrayServiceName = FormattableString.Invariant($"org.kde.StatusNotifierItem-{pid}-{tid}");
            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(_dbusMenuPath);

            try
            {
                _connection.RegisterObjectAsync(_statusNotifierItemDbusObj);
                _connection.RegisterServiceAsync(_sysTrayServiceName);
                _statusNotifierWatcher?.RegisterStatusNotifierItemAsync(_sysTrayServiceName);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, $"Error creating a DBus tray icon: {e}.");

                _serviceConnected = false;
            }

            _statusNotifierItemDbusObj.SetTitleAndTooltip(_tooltipText);
            _statusNotifierItemDbusObj.SetIcon(_icon);

            _statusNotifierItemDbusObj.ActivationDelegate += OnClicked;
        }

        private void DestroyTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierItemDbusObj is null)
                return;

            _connection.UnregisterObject(_statusNotifierItemDbusObj);
            _connection.UnregisterServiceAsync(_sysTrayServiceName);
        }

        public void Dispose()
        {
            IsActive = false;
            _isDisposed = true;
            DestroyTrayIcon();
            _connection?.Dispose();
            _serviceWatchDisposable?.Dispose();
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (_isDisposed || IconConverterDelegate is null)
                return;

            if (icon is null)
            {
                _statusNotifierItemDbusObj?.SetIcon(DbusPixmap.EmptyPixmap);
                return;
            }

            var x11iconData = IconConverterDelegate(icon);

            if (x11iconData.Length == 0) return;

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

            _icon = new DbusPixmap(w, h, pixByteArray);
            _statusNotifierItemDbusObj?.SetIcon(_icon);
        }

        public void SetIsVisible(bool visible)
        {
            if (_isDisposed)
                return;

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
    internal class StatusNotifierItemDbusObj : IStatusNotifierItem
    {
        private readonly StatusNotifierItemProperties _backingProperties;
        public event Action? OnTitleChanged;
        public event Action? OnIconChanged;
        public event Action? OnAttentionIconChanged;
        public event Action? OnOverlayIconChanged;
        public event Action? OnTooltipChanged;
        public Action<string>? NewStatusAsync { get; set; }
        public Action? ActivationDelegate { get; set; }
        public ObjectPath ObjectPath { get; }

        public StatusNotifierItemDbusObj(ObjectPath dbusmenuPath)
        {
            ObjectPath = new ObjectPath($"/StatusNotifierItem");

            _backingProperties = new StatusNotifierItemProperties
            {
                Menu = dbusmenuPath, // Needs a dbus menu somehow
                ToolTip = new ToolTip("")
            };

            InvalidateAll();
        }

        public Task ContextMenuAsync(int x, int y) => Task.CompletedTask;

        public Task ActivateAsync(int x, int y)
        {
            ActivationDelegate?.Invoke();
            return Task.CompletedTask;
        }

        public Task SecondaryActivateAsync(int x, int y) => Task.CompletedTask;

        public Task ScrollAsync(int delta, string orientation) => Task.CompletedTask;

        public void InvalidateAll()
        {
            OnTitleChanged?.Invoke();
            OnIconChanged?.Invoke();
            OnOverlayIconChanged?.Invoke();
            OnAttentionIconChanged?.Invoke();
            OnTooltipChanged?.Invoke();
        }

        public Task<IDisposable> WatchNewTitleAsync(Action handler, Action<Exception> onError)
        {
            OnTitleChanged += handler;
            return Task.FromResult(Disposable.Create(() => OnTitleChanged -= handler));
        }

        public Task<IDisposable> WatchNewIconAsync(Action handler, Action<Exception> onError)
        {
            OnIconChanged += handler;
            return Task.FromResult(Disposable.Create(() => OnIconChanged -= handler));
        }

        public Task<IDisposable> WatchNewAttentionIconAsync(Action handler, Action<Exception> onError)
        {
            OnAttentionIconChanged += handler;
            return Task.FromResult(Disposable.Create(() => OnAttentionIconChanged -= handler));
        }

        public Task<IDisposable> WatchNewOverlayIconAsync(Action handler, Action<Exception> onError)
        {
            OnOverlayIconChanged += handler;
            return Task.FromResult(Disposable.Create(() => OnOverlayIconChanged -= handler));
        }

        public Task<IDisposable> WatchNewToolTipAsync(Action handler, Action<Exception> onError)
        {
            OnTooltipChanged += handler;
            return Task.FromResult(Disposable.Create(() => OnTooltipChanged -= handler));
        }

        public Task<IDisposable> WatchNewStatusAsync(Action<string> handler, Action<Exception> onError)
        {
            NewStatusAsync += handler;
            return Task.FromResult(Disposable.Create(() => NewStatusAsync -= handler));
        }

        public Task<object?> GetAsync(string prop)
        {
            return Task.FromResult<object?>(prop switch
            {
                nameof(_backingProperties.Category) => _backingProperties.Category,
                nameof(_backingProperties.Id) => _backingProperties.Id,
                nameof(_backingProperties.Menu) => _backingProperties.Menu,
                nameof(_backingProperties.IconPixmap) => _backingProperties.IconPixmap,
                nameof(_backingProperties.Status) => _backingProperties.Status,
                nameof(_backingProperties.Title) => _backingProperties.Title,
                nameof(_backingProperties.ToolTip) => _backingProperties.ToolTip,
                _ => null
            });
        }

        public Task<StatusNotifierItemProperties> GetAllAsync() => Task.FromResult(_backingProperties);

        public Task SetAsync(string prop, object val) => Task.CompletedTask;

        public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler) =>
            Task.FromResult(Disposable.Empty);

        public void SetIcon(DbusPixmap dbusPixmap)
        {
            _backingProperties.IconPixmap = new[] { dbusPixmap };
            InvalidateAll();
        }

        public void SetTitleAndTooltip(string? text)
        {
            if (text is null)
                return;

            _backingProperties.Id = text;
            _backingProperties.Category = "ApplicationStatus";
            _backingProperties.Status = text;
            _backingProperties.Title = text;
            _backingProperties.ToolTip = new ToolTip(text);

            InvalidateAll();
        }
    }

    [DBusInterface("org.kde.StatusNotifierWatcher")]
    internal interface IStatusNotifierWatcher : IDBusObject
    {
        Task RegisterStatusNotifierItemAsync(string Service);
        Task RegisterStatusNotifierHostAsync(string Service);
    }

    [DBusInterface("org.kde.StatusNotifierItem")]
    internal interface IStatusNotifierItem : IDBusObject
    {
        Task ContextMenuAsync(int x, int y);
        Task ActivateAsync(int x, int y);
        Task SecondaryActivateAsync(int x, int y);
        Task ScrollAsync(int delta, string orientation);
        Task<IDisposable> WatchNewTitleAsync(Action handler, Action<Exception> onError);
        Task<IDisposable> WatchNewIconAsync(Action handler, Action<Exception> onError);
        Task<IDisposable> WatchNewAttentionIconAsync(Action handler, Action<Exception> onError);
        Task<IDisposable> WatchNewOverlayIconAsync(Action handler, Action<Exception> onError);
        Task<IDisposable> WatchNewToolTipAsync(Action handler, Action<Exception> onError);
        Task<IDisposable> WatchNewStatusAsync(Action<string> handler, Action<Exception> onError);
        Task<object?> GetAsync(string prop);
        Task<StatusNotifierItemProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    // This class is used by Tmds.Dbus to ferry properties
    // from the SNI spec.
    // Don't change this to actual C# properties since
    // Tmds.Dbus will get confused.
    [Dictionary]
    internal class StatusNotifierItemProperties
    {
        public string? Category;

        public string? Id;

        public string? Title;

        public string? Status;

        public ObjectPath Menu;

        public DbusPixmap[]? IconPixmap;

        public ToolTip ToolTip;
    }

    internal struct ToolTip
    {
        public readonly string First;
        public readonly DbusPixmap[] Second;
        public readonly string Third;
        public readonly string Fourth;

        private static readonly DbusPixmap[] s_blank =
        {
            new DbusPixmap(0, 0, Array.Empty<byte>()), new DbusPixmap(0, 0, Array.Empty<byte>())
        };

        public ToolTip(string message) : this("", s_blank, message, "")
        {
        }

        public ToolTip(string first, DbusPixmap[] second, string third, string fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }
    }

    internal readonly struct DbusPixmap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Data;

        public DbusPixmap(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        public static DbusPixmap EmptyPixmap = new DbusPixmap(1, 1, new byte[] { 255, 0, 0, 0 });
    }
}
