using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace Avalonia.FreeDesktop.DBusSystemTray
{
    public class DBusSysTray : IDisposable
    {
        private static int s_trayIconInstanceId = 0;
        private IStatusNotifierWatcher _statusNotifierWatcher;
        private string _sysTrayServiceName;
        private StatusNotifierItemDbusObj _statusNotifierItemDbusObj;
 
        private static int GetTID() => s_trayIconInstanceId++;

        public async void Initialize(ObjectPath dbusmenuPath)
        {
            var con = DBusHelper.Connection;

            _statusNotifierWatcher = con.CreateProxy<IStatusNotifierWatcher>("org.kde.StatusNotifierWatcher",
                "/StatusNotifierWatcher");

            var pid = Process.GetCurrentProcess().Id;
            var tid = GetTID();

            _sysTrayServiceName = $"org.kde.StatusNotifierItem-{pid}-{tid}";
            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(dbusmenuPath);

            await con.RegisterObjectAsync(_statusNotifierItemDbusObj);

            await con.RegisterServiceAsync(_sysTrayServiceName);

            await _statusNotifierWatcher.RegisterStatusNotifierItemAsync(_sysTrayServiceName);
         }

        public async void Dispose()
        {
            var con = DBusHelper.Connection;

            if (await con.UnregisterServiceAsync(_sysTrayServiceName))
            {
                con.UnregisterObject(_statusNotifierItemDbusObj);
            }
        }

        public void SetIcon(DbusPixmap dbusPixmap)
        {
            _statusNotifierItemDbusObj.SetIcon(dbusPixmap);
        }

        public void SetTitleAndTooltip(string text)
        {
            _statusNotifierItemDbusObj.SetTitleAndTooltip(text);
        }

        public void SetActivationDelegate(Action activationDelegate)
        {
            _statusNotifierItemDbusObj.ActivationDelegate = activationDelegate;
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
        private event Action<PropertyChanges> OnPropertyChange;
        public event Action OnTitleChanged;
        public event Action OnIconChanged;
        public event Action OnAttentionIconChanged;
        public event Action OnOverlayIconChanged;
        public event Action OnTooltipChanged;
        public Action<INativeMenuExporter> SetNativeMenuExporter { get; set; }
        public Action<string> NewStatusAsync { get; set; }
        public Action ActivationDelegate { get; set; }
        public ObjectPath ObjectPath { get; }

        public StatusNotifierItemDbusObj(ObjectPath dbusmenuPath)
        {
            var ID = Guid.NewGuid().ToString().Replace("-", "");
            ObjectPath = new ObjectPath($"/StatusNotifierItem");
            
            _backingProperties = new StatusNotifierItemProperties
            {
                Menu = dbusmenuPath, // Needs a dbus menu somehow
                ToolTip = new ToolTip("")
            };

            InvalidateAll();
        }

        public async Task ContextMenuAsync(int X, int Y)
        {
        }

        public async Task ActivateAsync(int X, int Y)
        {
            ActivationDelegate?.Invoke();
        }

        public async Task SecondaryActivateAsync(int X, int Y)
        {
        }

        public async Task ScrollAsync(int Delta, string Orientation)
        {
        }

        public void InvalidateAll()
        {
            OnTitleChanged?.Invoke();
            OnIconChanged?.Invoke();
            OnOverlayIconChanged?.Invoke();
            OnAttentionIconChanged?.Invoke();
            OnTooltipChanged?.Invoke();
        }

        public async Task<IDisposable> WatchNewTitleAsync(Action handler, Action<Exception> onError = null)
        {
            OnTitleChanged += handler;
            return Disposable.Create(() => OnTitleChanged -= handler);
        }

        public async Task<IDisposable> WatchNewIconAsync(Action handler, Action<Exception> onError = null)
        {
            OnIconChanged += handler;
            return Disposable.Create(() => OnIconChanged -= handler);
        }

        public async Task<IDisposable> WatchNewAttentionIconAsync(Action handler, Action<Exception> onError = null)
        {
            OnAttentionIconChanged += handler;
            return Disposable.Create(() => OnAttentionIconChanged -= handler);
        }

        public async Task<IDisposable> WatchNewOverlayIconAsync(Action handler, Action<Exception> onError = null)
        {
            OnOverlayIconChanged += handler;
            return Disposable.Create(() => OnOverlayIconChanged -= handler);
        }

        public async Task<IDisposable> WatchNewToolTipAsync(Action handler, Action<Exception> onError = null)
        {
            OnTooltipChanged += handler;
            return Disposable.Create(() => OnTooltipChanged -= handler);
        }

        public async Task<IDisposable> WatchNewStatusAsync(Action<string> handler, Action<Exception> onError = null)
        {
            NewStatusAsync += handler;
            return Disposable.Create(() => NewStatusAsync -= handler);
        }

        public async Task<object> GetAsync(string prop)
        {
            if (prop.Contains("Menu"))
            {
                return _backingProperties.Menu;
            }

            return default;
        }

        public async Task<StatusNotifierItemProperties> GetAllAsync()
        {
            return _backingProperties;
        }

        public Task SetAsync(string prop, object val) => Task.CompletedTask;

        public async Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            OnPropertyChange += handler;
            return Disposable.Create(() => OnPropertyChange -= handler);
        }

        public void SetIcon(DbusPixmap dbusPixmap)
        {
            _backingProperties.IconPixmap = new[] { dbusPixmap };
            InvalidateAll();
        }

        public void SetTitleAndTooltip(string text)
        {
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
    interface IStatusNotifierItem : IDBusObject
    {
        Task ContextMenuAsync(int X, int Y);
        Task ActivateAsync(int X, int Y);
        Task SecondaryActivateAsync(int X, int Y);
        Task ScrollAsync(int Delta, string Orientation);
        Task<IDisposable> WatchNewTitleAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchNewIconAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchNewAttentionIconAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchNewOverlayIconAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchNewToolTipAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchNewStatusAsync(Action<string> handler, Action<Exception> onError = null);
        Task<object> GetAsync(string prop);
        Task<StatusNotifierItemProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    internal class StatusNotifierItemProperties
    {
        public string Category;

        public string Id;

        public string Title;

        public string Status;

        public int WindowId;

        public string IconThemePath;

        public ObjectPath Menu;

        public bool ItemIsMenu;

        public string IconName;

        public DbusPixmap[] IconPixmap;

        public string OverlayIconName;

        public DbusPixmap[] OverlayIconPixmap;

        public string AttentionIconName;

        public DbusPixmap[] AttentionIconPixmap;

        public string AttentionMovieName;

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
            new DbusPixmap(0, 0, new byte[] { }), new DbusPixmap(0, 0, new byte[] { })
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

    public readonly struct DbusPixmap
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
