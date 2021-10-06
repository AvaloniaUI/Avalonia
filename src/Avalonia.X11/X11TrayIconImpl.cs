#nullable enable

using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.Logging;
using Avalonia.Platform;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]

namespace Avalonia.X11
{
    internal class X11TrayIconImpl : ITrayIconImpl
    {
        private static int trayIconInstanceId;
        private readonly ObjectPath _dbusMenuPath;
        private StatusNotifierItemDbusObj? _statusNotifierItemDbusObj;
        private readonly Connection? _connection;
        private DbusPixmap _icon;

        private IStatusNotifierWatcher? _statusNotifierWatcher;

        private string? _sysTrayServiceName;
        private string? _tooltipText;
        private bool _isActive;
        private bool _isDisposed;
        private readonly bool _ctorFinished;

        public INativeMenuExporter? MenuExporter { get; }
        public Action? OnClicked { get; set; }

        public X11TrayIconImpl()
        {
            _connection = DBusHelper.TryGetConnection();

            if (_connection is null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)
                    ?.Log(this, "Unable to get a dbus connection for system tray icons.");
                return;
            }

            _dbusMenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;
            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(_dbusMenuPath, _connection);
            CreateTrayIcon();
            _ctorFinished = true;
        }

        public async void CreateTrayIcon()
        {
            if (_connection is null) return;

            try
            {
                _statusNotifierWatcher = _connection.CreateProxy<IStatusNotifierWatcher>(
                    "org.kde.StatusNotifierWatcher",
                    "/StatusNotifierWatcher");
            }
            catch (Exception)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)
                    ?.Log(this,
                        "DBUS: org.kde.StatusNotifierWatcher service is not available on this system. System Tray Icons will not work without it.");
            }

            if (_statusNotifierWatcher is null) return;

            var pid = Process.GetCurrentProcess().Id;
            var tid = trayIconInstanceId++;

            _sysTrayServiceName = $"org.kde.StatusNotifierItem-{pid}-{tid}";
            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(_dbusMenuPath);

            await _connection.RegisterObjectAsync(_statusNotifierItemDbusObj);

            await _connection.RegisterServiceAsync(_sysTrayServiceName);

            await _statusNotifierWatcher.RegisterStatusNotifierItemAsync(_sysTrayServiceName);

            _statusNotifierItemDbusObj.SetTitleAndTooltip(_tooltipText);
            _statusNotifierItemDbusObj.SetIcon(_icon);

            _statusNotifierItemDbusObj.ActivationDelegate = () =>
            {
                OnClicked?.Invoke();
            };

            _isActive = true;
        }

        public async void DestroyTrayIcon()
        {
            if (_connection is null) return;
            _connection.UnregisterObject(_statusNotifierItemDbusObj);
            await _connection.UnregisterServiceAsync(_sysTrayServiceName);
            _isActive = false;
        }

        public void Dispose()
        {
            _isDisposed = true;
            DestroyTrayIcon();
            _connection?.Dispose();
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (_isDisposed) return;
            if (!(icon is X11IconData x11icon)) return;

            var w = (int)x11icon.Data[0];
            var h = (int)x11icon.Data[1];

            var pixLength = w * h;
            var pixByteArrayCounter = 0;
            var pixByteArray = new byte[w * h * 4];

            for (var i = 0; i < pixLength; i++)
            {
                var rawPixel = x11icon.Data[i + 2].ToUInt32();
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
            if (_isDisposed || !_ctorFinished) return;

            if (visible & !_isActive)
            {
                DestroyTrayIcon();
                CreateTrayIcon();
            }
            else if (!visible & _isActive)
            {
                DestroyTrayIcon();
            }
        }

        public void SetToolTipText(string? text)
        {
            if (_isDisposed || text is null) return;
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

        public Task<object> GetAsync(string prop) => Task.FromResult(new object());

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
            if (text is null) return;

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
        Task<object> GetAsync(string prop);
        Task<StatusNotifierItemProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    /// This class is used by Tmds.Dbus to ferry properties
    /// from the SNI spec.
    /// Don't change this to actual C# properties since
    /// Tmds.Dbus will get confused.
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
    }
}
