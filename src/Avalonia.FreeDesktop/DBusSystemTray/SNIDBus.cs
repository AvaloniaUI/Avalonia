using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace Avalonia.FreeDesktop.DBusSystemTray
{
    public class SNIDBus : IDisposable
    {
        public SNIDBus()
        {
        }

        private static int trayinstanceID = 0;
        private IStatusNotifierWatcher _snw;
        private string _sysTraySrvName;
        private StatusNotifierItem _statusNotifierItem;

        private static int GetTID()
        {
            trayinstanceID = 4;
            return trayinstanceID;
        }

        public async void Initialize()
        {
            var x = Process.GetCurrentProcess().Id;
            var y = GetTID();

            _sysTraySrvName = $"org.kde.StatusNotifierItem-{x}-{y}";
            _statusNotifierItem = new StatusNotifierItem();
            var con = DBusHelper.Connection;

            await con.RegisterObjectAsync(_statusNotifierItem);

            await con.RegisterServiceAsync(_sysTraySrvName, () =>
            {
            });

            while (!await con.IsServiceActiveAsync(_sysTraySrvName))
            {
                await Task.Delay(150);
            }

            var yx = con.CreateProxy<IStatusNotifierItem>(_sysTraySrvName, _statusNotifierItem.ObjectPath);

            _snw =
                DBusHelper.Connection.CreateProxy<IStatusNotifierWatcher>("org.kde.StatusNotifierWatcher",
                    "/StatusNotifierWatcher");

            while (!await DBusHelper.Connection.IsServiceActiveAsync("org.kde.StatusNotifierWatcher"))
            {
                await Task.Delay(150);
            }

            await _snw.RegisterStatusNotifierItemAsync(_sysTraySrvName);
            //
            // Task.Run(async () =>
            // {
            //     await Task.Delay(2000);
            //     tx.InvalidateAll();
            // });
        }

        public async void Dispose()
        {
            var con = DBusHelper.Connection;

            if (await con.UnregisterServiceAsync(_sysTraySrvName))
            {
                con.UnregisterObject(_statusNotifierItem);
            }
        }

        public void SetIcon(Pixmap pixmap)
        {
            _statusNotifierItem.SetIcon(pixmap);
        }
    }

    internal class StatusNotifierItem : IStatusNotifierItem
    {
        private event Action<PropertyChanges> OnPropertyChange;

        public event Action OnTitleChanged;
        public event Action OnIconChanged;
        public event Action OnAttentionIconChanged;
        public event Action OnOverlayIconChanged;


        public Action NewToolTipAsync;
        public ObjectPath ObjectPath { get; }

        readonly StatusNotifierItemProperties props;

        public StatusNotifierItem()
        {
            var ID = Guid.NewGuid().ToString().Replace("-", "");
            ObjectPath = new ObjectPath($"/StatusNotifierItem");

            props = new StatusNotifierItemProperties
            {
                Title = "Avalonia Test Tray",
                Status = "Avalonia Test Tray",
                Id = "Avalonia Test Tray",
                AttentionIconPixmap = new[] { new Pixmap(0, 0, new byte[] {  }), new Pixmap(0, 0, new byte[] {  }) },
                // IconPixmap = new[] { new Pixmap(1, 1, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) }
            };
        }

        public async Task ContextMenuAsync(int X, int Y)
        {
        }

        public async Task ActivateAsync(int X, int Y)
        {
            // OnPropertyChange?.Invoke(new PropertyChanges());
        }

        public async Task SecondaryActivateAsync(int X, int Y)
        {
            //throw new NotImplementedException();5
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
            NewToolTipAsync += handler;
            return Disposable.Create(() => NewToolTipAsync -= handler);
        }

        public async Task<IDisposable> WatchNewStatusAsync(Action<string> handler, Action<Exception> onError = null)
        {
            NewStatusAsync += handler;
            return Disposable.Create(() => NewStatusAsync -= handler);
        }

        public async Task<object> GetAsync(string prop)
        {
            return prop switch
            {
                "Category" => props.Category,
                "Id" => props.Id,
                "Title" => props.Title,
                "Status" => props.Status,
                "WindowId" => props.WindowId,
                "IconThemePath" => props.IconThemePath,
                "ItemIsMenu" => props.ItemIsMenu,
                "IconName" => props.IconName,
                "IconPixmap" => props.IconPixmap,
                "OverlayIconName" => props.OverlayIconName,
                "OverlayIconPixmap" => props.OverlayIconPixmap,
                "AttentionIconName" => props.AttentionIconName,
                "AttentionIconPixmap" => props.AttentionIconPixmap,
                "AttentionMovieName" => props.AttentionMovieName,
                "ToolTip" => props.ToolTip,
                _ => default
            };
        }

        public async Task<StatusNotifierItemProperties> GetAllAsync()
        {
            return props;
        }

        public Action<string> NewStatusAsync { get; set; }

        public Task SetAsync(string prop, object val) => Task.CompletedTask;

        public async Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            OnPropertyChange += handler;
            return Disposable.Create(() => OnPropertyChange -= handler);
        }

        public void SetIcon(Pixmap pixmap)
        {
           props.IconPixmap = new[] { pixmap };
           InvalidateAll();
        }
    }

    [DBusInterface("org.kde.StatusNotifierWatcher")]
    interface IStatusNotifierWatcher : IDBusObject
    {
        Task RegisterStatusNotifierItemAsync(string Service);
        Task RegisterStatusNotifierHostAsync(string Service);

        Task<IDisposable> WatchStatusNotifierItemRegisteredAsync(Action<string> handler,
            Action<Exception> onError = null);

        Task<IDisposable> WatchStatusNotifierItemUnregisteredAsync(Action<string> handler,
            Action<Exception> onError = null);

        Task<IDisposable> WatchStatusNotifierHostRegisteredAsync(Action handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<StatusNotifierWatcherProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class StatusNotifierWatcherProperties
    {
        public string[] RegisteredStatusNotifierItems;

        public bool IsStatusNotifierHostRegistered;

        public int ProtocolVersion;
    }

    static class StatusNotifierWatcherExtensions
    {
        public static Task<string[]> GetRegisteredStatusNotifierItemsAsync(this IStatusNotifierWatcher o) =>
            o.GetAsync<string[]>("RegisteredStatusNotifierItems");

        public static Task<bool> GetIsStatusNotifierHostRegisteredAsync(this IStatusNotifierWatcher o) =>
            o.GetAsync<bool>("IsStatusNotifierHostRegistered");

        public static Task<int> GetProtocolVersionAsync(this IStatusNotifierWatcher o) =>
            o.GetAsync<int>("ProtocolVersion");
    }

    [DBusInterface("org.gtk.Actions")]
    interface IActions : IDBusObject
    {
        Task<string[]> ListAsync();
        Task<(bool description, Signature, object[])> DescribeAsync(string ActionName);
        Task<IDictionary<string, (bool, Signature, object[])>> DescribeAllAsync();
        Task ActivateAsync(string ActionName, object[] Parameter, IDictionary<string, object> PlatformData);
        Task SetStateAsync(string ActionName, object Value, IDictionary<string, object> PlatformData);

        Task<IDisposable> WatchChangedAsync(
            Action<(string[] removals, IDictionary<string, bool> enableChanges, IDictionary<string, object> stateChanges
                , IDictionary<string, (bool, Signature, object[])> additions)> handler,
            Action<Exception> onError = null);
    }

    [DBusInterface("org.gtk.Application")]
    interface IApplication : IDBusObject
    {
        Task ActivateAsync(IDictionary<string, object> PlatformData);
        Task OpenAsync(string[] Uris, string Hint, IDictionary<string, object> PlatformData);
        Task<int> CommandLineAsync(ObjectPath Path, byte[][] Arguments, IDictionary<string, object> PlatformData);
        Task<T> GetAsync<T>(string prop);
        Task<ApplicationProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class ApplicationProperties
    {
        public bool Busy;
    }

    static class ApplicationExtensions
    {
        public static Task<bool> GetBusyAsync(this IApplication o) => o.GetAsync<bool>("Busy");
    }

    [DBusInterface("org.freedesktop.Application")]
    interface IApplication0 : IDBusObject
    {
        Task ActivateAsync(IDictionary<string, object> PlatformData);
        Task OpenAsync(string[] Uris, IDictionary<string, object> PlatformData);
        Task ActivateActionAsync(string ActionName, object[] Parameter, IDictionary<string, object> PlatformData);
    }

    [DBusInterface("org.gnome.Sysprof3.Profiler")]
    interface IProfiler : IDBusObject
    {
        Task StartAsync(IDictionary<string, object> Options, CloseSafeHandle Fd);
        Task StopAsync();
        Task<T> GetAsync<T>(string prop);
        Task<ProfilerProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class ProfilerProperties
    {
        public IDictionary<string, object> Capabilities;
    }

    static class ProfilerExtensions
    {
        public static Task<IDictionary<string, object>> GetCapabilitiesAsync(this IProfiler o) =>
            o.GetAsync<IDictionary<string, object>>("Capabilities");
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
    class StatusNotifierItemProperties
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

        public Pixmap[] IconPixmap;

        public string OverlayIconName;

        public Pixmap[] OverlayIconPixmap;

        public string AttentionIconName;

        public Pixmap[] AttentionIconPixmap;

        public string AttentionMovieName;

        public ToolTip ToolTip;
    }

    public readonly struct ToolTip
    {
        public readonly string First;
        public readonly Pixmap[] Second;
        public readonly string Third;
        public readonly string Fourth;

        public ToolTip(string first, Pixmap[] second, string third, string fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }
    }


    public readonly struct Pixmap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Data;

        public Pixmap(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }
    }
}
