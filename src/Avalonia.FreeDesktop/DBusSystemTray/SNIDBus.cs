using System;
using System.Collections.Generic;
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

       public async void Initialize()
        {

            var serviceName = $"AvaloniaSNITest_{Guid.NewGuid()}";

            var path =
                 Connection.Session.CreateProxy<IStatusNotifierWatcher>("org.kde.StatusNotifierWatcher",
                    "/StatusNotifierWatcher");
            
             await path.RegisterStatusNotifierHostAsync(serviceName);

             await path.WatchPropertiesAsync(x =>
             {

             });

             await path.WatchStatusNotifierHostRegisteredAsync(() =>
             {

             }, z =>
             {

             });
             
             

        }
    }
    
    [DBusInterface("org.kde.StatusNotifierWatcher")]
    interface IStatusNotifierWatcher : IDBusObject
    {
        Task RegisterStatusNotifierItemAsync(string Service);
        Task RegisterStatusNotifierHostAsync(string Service);
        Task<IDisposable> WatchStatusNotifierItemRegisteredAsync(Action<string> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchStatusNotifierItemUnregisteredAsync(Action<string> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchStatusNotifierHostRegisteredAsync(Action handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<StatusNotifierWatcherProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class StatusNotifierWatcherProperties
    {
        private string[] _RegisteredStatusNotifierItems = default(string[]);
        public string[] RegisteredStatusNotifierItems
        {
            get
            {
                return _RegisteredStatusNotifierItems;
            }

            set
            {
                _RegisteredStatusNotifierItems = (value);
            }
        }

        private bool _IsStatusNotifierHostRegistered = default(bool);
        public bool IsStatusNotifierHostRegistered
        {
            get
            {
                return _IsStatusNotifierHostRegistered;
            }

            set
            {
                _IsStatusNotifierHostRegistered = (value);
            }
        }

        private int _ProtocolVersion = default(int);
        public int ProtocolVersion
        {
            get
            {
                return _ProtocolVersion;
            }

            set
            {
                _ProtocolVersion = (value);
            }
        }
    }

    static class StatusNotifierWatcherExtensions
    {
        public static Task<string[]> GetRegisteredStatusNotifierItemsAsync(this IStatusNotifierWatcher o) => o.GetAsync<string[]>("RegisteredStatusNotifierItems");
        public static Task<bool> GetIsStatusNotifierHostRegisteredAsync(this IStatusNotifierWatcher o) => o.GetAsync<bool>("IsStatusNotifierHostRegistered");
        public static Task<int> GetProtocolVersionAsync(this IStatusNotifierWatcher o) => o.GetAsync<int>("ProtocolVersion");
    }

    [DBusInterface("org.gtk.Actions")]
    interface IActions : IDBusObject
    {
        Task<string[]> ListAsync();
        Task<(bool description, Signature, object[])> DescribeAsync(string ActionName);
        Task<IDictionary<string, (bool, Signature, object[])>> DescribeAllAsync();
        Task ActivateAsync(string ActionName, object[] Parameter, IDictionary<string, object> PlatformData);
        Task SetStateAsync(string ActionName, object Value, IDictionary<string, object> PlatformData);
        Task<IDisposable> WatchChangedAsync(Action<(string[] removals, IDictionary<string, bool> enableChanges, IDictionary<string, object> stateChanges, IDictionary<string, (bool, Signature, object[])> additions)> handler, Action<Exception> onError = null);
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
        private bool _Busy = default(bool);
        public bool Busy
        {
            get
            {
                return _Busy;
            }

            set
            {
                _Busy = (value);
            }
        }
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
        private IDictionary<string, object> _Capabilities = default(IDictionary<string, object>);
        public IDictionary<string, object> Capabilities
        {
            get
            {
                return _Capabilities;
            }

            set
            {
                _Capabilities = (value);
            }
        }
    }

    static class ProfilerExtensions
    {
        public static Task<IDictionary<string, object>> GetCapabilitiesAsync(this IProfiler o) => o.GetAsync<IDictionary<string, object>>("Capabilities");
    }
}
