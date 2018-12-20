using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.FreeDesktop.DBusMenu;
using Tmds.DBus;
#pragma warning disable 1998
#pragma warning disable 67
namespace Avalonia.FreeDesktop
{
    public class DBusExportedMenu : IDBusObject, IDisposable, IDBusMenu
    {
        private static readonly IRegistrar s_registar = Connection.Session.CreateProxy<IRegistrar>(
            "com.canonical.AppMenu.Registrar",
            "/com/canonical/AppMenu/Registrar");
        private readonly AvaloniaList<SimpleMenuItem> _items;
        private int? _xid;
        public ObjectPath ObjectPath { get; }

        public DBusExportedMenu(AvaloniaList<SimpleMenuItem> items)
        {
            _items = items;
            ObjectPath = new ObjectPath("/net/avaloniaui/dbusmenu/"
                                        + Guid.NewGuid().ToString().Replace("-", ""));
        }

        public async Task RegisterAsync(int? xid)
        {
            _xid = xid;
            await DBusHelper.Connection.RegisterObjectAsync(this);
            if (xid.HasValue)
                await s_registar.RegisterWindowAsync((uint)xid.Value, ObjectPath);
        }
        
        public async void Dispose()
        {
            if (_xid.HasValue)
                await s_registar.UnregisterWindowAsync((uint)_xid.Value);
            DBusHelper.Connection.UnregisterObject(this);
        }

        Task<(uint revision, (int, IDictionary<string, object>, object[]) layout)> IDBusMenu.GetLayoutAsync(int ParentId, int RecursionDepth, string[] PropertyNames)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task<(int, IDictionary<string, object>)[]> IDBusMenu.GetGroupPropertiesAsync(int[] Ids, string[] PropertyNames)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task<object> IDBusMenu.GetPropertyAsync(int Id, string Name)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task IDBusMenu.EventAsync(int Id, string EventId, object Data, uint Timestamp)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task<int[]> IDBusMenu.EventGroupAsync((int, string, object, uint)[] Events)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task<bool> IDBusMenu.AboutToShowAsync(int Id)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task<(int[] updatesNeeded, int[] idErrors)> IDBusMenu.AboutToShowGroupAsync(int[] Ids)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }


        Task<object> IDBusMenu.GetAsync(string prop)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task<DBusMenuProperties> IDBusMenu.GetAllAsync()
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        Task IDBusMenu.SetAsync(string prop, object val)
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }

        #region Events

        private event Action<((int, IDictionary<string, object>)[] updatedProps, (int, string[])[] removedProps)>
            ItemsPropertiesUpdated;
        private event Action<(uint revision, int parent)> LayoutUpdated;
        private event Action<(int id, uint timestamp)> ItemActivationRequested;
        private event Action<PropertyChanges> PropertiesChanged;
        
        async Task<IDisposable> IDBusMenu.WatchItemsPropertiesUpdatedAsync(Action<((int, IDictionary<string, object>)[] updatedProps, (int, string[])[] removedProps)> handler, Action<Exception> onError)
        {
            ItemsPropertiesUpdated += handler;
            return Disposable.Create(() => ItemsPropertiesUpdated -= handler);
        }
        async Task<IDisposable> IDBusMenu.WatchLayoutUpdatedAsync(Action<(uint revision, int parent)> handler, Action<Exception> onError)
        {
            LayoutUpdated += handler;
            return Disposable.Create(() => LayoutUpdated -= handler);
        }

        async Task<IDisposable> IDBusMenu.WatchItemActivationRequestedAsync(Action<(int id, uint timestamp)> handler, Action<Exception> onError)
        {
            ItemActivationRequested+= handler;
            return Disposable.Create(() => ItemActivationRequested -= handler);
        }

        async Task<IDisposable> IDBusMenu.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            PropertiesChanged += handler;
            return Disposable.Create(() => PropertiesChanged -= handler);
        }

        #endregion
    }
}
