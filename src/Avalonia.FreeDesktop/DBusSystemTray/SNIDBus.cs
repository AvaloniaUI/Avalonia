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
    public class SNIDBus
    {
        public SNIDBus()
        {
        }

        private static int trayinstanceID = 0;

        private static int GetTID()
        {
            trayinstanceID = (int)new Random().Next(0, 100);
            return trayinstanceID;
        }

        public async void Initialize()
        {
            var x = Process.GetCurrentProcess().Id;
            var y = GetTID();

            var sysTraySrvName = $"org.kde.StatusNotifierItem-{x}-{y}";
            var tx = new StatusNotifierItem();

            await DBusHelper.Connection.RegisterObjectAsync(tx);

            await DBusHelper.Connection.RegisterServiceAsync(sysTraySrvName, () =>
            {
            });

            while (!await DBusHelper.Connection.IsServiceActiveAsync(sysTraySrvName))
            {
                await Task.Delay(1000);
            }

            var yx = DBusHelper.Connection.CreateProxy<IStatusNotifierItem>(sysTraySrvName, tx.ObjectPath);

            var snw =
                DBusHelper.Connection.CreateProxy<IStatusNotifierWatcher>("org.kde.StatusNotifierWatcher",
                    "/StatusNotifierWatcher");

            while (!await DBusHelper.Connection.IsServiceActiveAsync("org.kde.StatusNotifierWatcher"))
            {
                await Task.Delay(1000);
            }

            await snw.RegisterStatusNotifierItemAsync(sysTraySrvName);

            tx.Ready();

            await yx.ActivateAsync(1, 1);
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
            props = new StatusNotifierItemProperties();

            props.Title = "Avalonia Test Tray";
            props.Id = ID;
//     
            //     
            // public string Category { get; set; } = default;
            //
            // public string Id { get; set; } = default;
            //
            // public string Title { get; set; } = default;
            //
            // public string Status { get; set; } = default;
            //
            // public int WindowId { get; set; } = default;
        }


        // static class StatusNotifierItemExtensions
        // {
        //     public static Task<string> GetCategoryAsync(this IStatusNotifierItem o) => o.GetAsync<string>("Category");
        //     public static Task<string> GetIdAsync(this IStatusNotifierItem o) => o.GetAsync<string>("Id");
        //     public static Task<string> GetTitleAsync(this IStatusNotifierItem o) => o.GetAsync<string>("Title");
        //     public static Task<string> GetStatusAsync(this IStatusNotifierItem o) => o.GetAsync<string>("Status");
        //     public static Task<int> GetWindowIdAsync(this IStatusNotifierItem o) => o.GetAsync<int>("WindowId");
        //
        //     public static Task<string> GetIconThemePathAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<string>("IconThemePath");
        //
        //     public static Task<ObjectPath> GetMenuAsync(this IStatusNotifierItem o) => o.GetAsync<ObjectPath>("Menu");
        //     public static Task<bool> GetItemIsMenuAsync(this IStatusNotifierItem o) => o.GetAsync<bool>("ItemIsMenu");
        //     public static Task<string> GetIconNameAsync(this IStatusNotifierItem o) => o.GetAsync<string>("IconName");
        //
        //     public static Task<(int, int, byte[])[]> GetIconPixmapAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<(int, int, byte[])[]>("IconPixmap");
        //
        //     public static Task<string> GetOverlayIconNameAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<string>("OverlayIconName");
        //
        //     public static Task<(int, int, byte[])[]> GetOverlayIconPixmapAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<(int, int, byte[])[]>("OverlayIconPixmap");
        //
        //     public static Task<string> GetAttentionIconNameAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<string>("AttentionIconName");
        //
        //     public static Task<(int, int, byte[])[]> GetAttentionIconPixmapAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<(int, int, byte[])[]>("AttentionIconPixmap");
        //
        //     public static Task<string> GetAttentionMovieNameAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<string>("AttentionMovieName");
        //
        //     public static Task<(string, (int, int, byte[])[], string, string)>
        //         GetToolTipAsync(this IStatusNotifierItem o) =>
        //         o.GetAsync<(string, (int, int, byte[])[], string, string)>("ToolTip");
        // }

        public async Task ContextMenuAsync(int X, int Y)
        {
        }

        public async Task ActivateAsync(int X, int Y)
        {
            // OnPropertyChange?.Invoke(new PropertyChanges());
        }

        public async Task SecondaryActivateAsync(int X, int Y)
        {
            throw new NotImplementedException();
        }

        public async Task ScrollAsync(int Delta, string Orientation)
        {
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

        public Action<string> NewStatusAsync { get; set; }

        public async Task<StatusNotifierItemProperties> GetAllAsync()
        {
            return props;
        }

        public async Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            OnPropertyChange += handler;
            return Disposable.Create(() => OnPropertyChange -= handler);
        }

        public void Ready()
        {
            OnTitleChanged?.Invoke();
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
        public string[] RegisteredStatusNotifierItems { get; set; } = default;

        public bool IsStatusNotifierHostRegistered { get; set; } = default;

        public int ProtocolVersion { get; set; } = default;
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
        public bool Busy { get; set; } = default;
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
        public IDictionary<string, object> Capabilities { get; set; } = default;
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
        Task<StatusNotifierItemProperties> GetAllAsync();
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }


    [Dictionary]
    class StatusNotifierItemProperties
    {
        public string Category { get; set; } = default;

        public string Id { get; set; } = default;

        public string Title { get; set; } = default;

        public string Status { get; set; } = default;

        public int WindowId { get; set; } = default;

        public string IconThemePath { get; set; } = default;

        public ObjectPath Menu { get; set; } = default;

        public bool ItemIsMenu { get; set; } = default;

        public string IconName { get; set; } = default;

        public (int, int, byte[])[] IconPixmap { get; set; } = default;

        public string OverlayIconName { get; set; } = default;

        public (int, int, byte[])[] OverlayIconPixmap { get; set; } = default;

        public string AttentionIconName { get; set; } = default;

        public (int, int, byte[])[] AttentionIconPixmap { get; set; } = default;

        public string AttentionMovieName { get; set; } = default;

        public (string, (int, int, byte[])[], string, string) ToolTip { get; set; } = default;
    }
}
