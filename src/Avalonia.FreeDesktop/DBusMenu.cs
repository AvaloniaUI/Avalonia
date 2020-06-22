
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace Avalonia.FreeDesktop.DBusMenu
{

    [DBusInterface("org.freedesktop.DBus.Properties")]
    interface IFreeDesktopDBusProperties : IDBusObject
    {
        Task<object> GetAsync(string prop);
        Task<DBusMenuProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [DBusInterface("com.canonical.dbusmenu")]
    interface IDBusMenu : IFreeDesktopDBusProperties
    {
        Task<(uint revision, (int, KeyValuePair<string, object>[], object[]) layout)> GetLayoutAsync(int ParentId, int RecursionDepth, string[] PropertyNames);
        Task<(int, KeyValuePair<string, object>[])[]> GetGroupPropertiesAsync(int[] Ids, string[] PropertyNames);
        Task<object> GetPropertyAsync(int Id, string Name);
        Task EventAsync(int Id, string EventId, object Data, uint Timestamp);
        Task<int[]> EventGroupAsync((int id, string eventId, object data, uint timestamp)[] events);
        Task<bool> AboutToShowAsync(int Id);
        Task<(int[] updatesNeeded, int[] idErrors)> AboutToShowGroupAsync(int[] Ids);
        Task<IDisposable> WatchItemsPropertiesUpdatedAsync(Action<((int, IDictionary<string, object>)[] updatedProps, (int, string[])[] removedProps)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint revision, int parent)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchItemActivationRequestedAsync(Action<(int id, uint timestamp)> handler, Action<Exception> onError = null);
    }

    [Dictionary]
    class DBusMenuProperties
    {
        public uint Version { get; set; } = default (uint);
        public string TextDirection { get; set; } = default (string);
        public string Status { get; set; } = default (string);
        public string[] IconThemePath { get; set; } = default (string[]);
    }


    [DBusInterface("com.canonical.AppMenu.Registrar")]
    interface IRegistrar : IDBusObject
    {
        Task RegisterWindowAsync(uint WindowId, ObjectPath MenuObjectPath);
        Task UnregisterWindowAsync(uint WindowId);
        Task<(string service, ObjectPath menuObjectPath)> GetMenuForWindowAsync(uint WindowId);
        Task<(uint, string, ObjectPath)[]> GetMenusAsync();
        Task<IDisposable> WatchWindowRegisteredAsync(Action<(uint windowId, string service, ObjectPath menuObjectPath)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchWindowUnregisteredAsync(Action<uint> handler, Action<Exception> onError = null);
    }
}
