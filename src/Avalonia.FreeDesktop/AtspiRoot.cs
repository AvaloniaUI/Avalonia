using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.FreeDesktop.Atspi;
using Avalonia.Logging;
using Tmds.DBus;

#nullable enable

namespace Avalonia.FreeDesktop
{
    public class AtspiRoot : IAccessible, IApplication
    {
        private Connection? _connection;

        public AtspiRoot(Connection sessionConnection)
        {
            Register(sessionConnection);
        }

        ObjectPath IDBusObject.ObjectPath => "/accessible/root";

        public static AtspiRoot? TryCreate()
        {
            return DBusHelper.Connection != null ? new AtspiRoot(DBusHelper.Connection) : null;
        }

        private async void Register(Connection sessionConnection)
        {
            const string rootPath = "/org/a11y/atspi/accessible/root";
            
            try
            {
                // Get the address of the a11y bus and open a connection to it.
                var bus = sessionConnection.CreateProxy<IBus>("org.a11y.Bus", "/org/a11y/bus");
                var address = await bus.GetAddressAsync();
                var connection = new Connection(address);
                var connectionInfo = await connection.ConnectAsync();

                // Register the org.a11y.atspi.Application and org.a11y.atspi.Accessible interfaces at the well-known
                // object path
                await connection.RegisterObjectAsync(this);
            
                // Register ourselves on the a11y bus.
                var socket = connection.CreateProxy<ISocket>("org.a11y.atspi.Registry", rootPath);
                var plug = (connectionInfo.LocalName, rootPath);
                var (desktopName, desktopPath) = await socket.EmbedAsync(plug);
                _connection = connection;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "Error connecting to AT-SPI bus: {Error}", e);
            }
        }

        Task<(string, ObjectPath)> IAccessible.GetChildAtIndexAsync(int Index)
        {
            throw new NotImplementedException();
        }

        Task<(string, ObjectPath)[]> IAccessible.GetChildrenAsync()
        {
            throw new NotImplementedException();
        }

        Task<int> IAccessible.GetIndexInParentAsync()
        {
            throw new NotImplementedException();
        }

        Task<(uint, (string, ObjectPath)[])[]> IAccessible.GetRelationSetAsync()
        {
            throw new NotImplementedException();
        }

        Task<uint> IAccessible.GetRoleAsync()
        {
            throw new NotImplementedException();
        }

        Task<string> IAccessible.GetRoleNameAsync()
        {
            throw new NotImplementedException();
        }

        Task<string> IAccessible.GetLocalizedRoleNameAsync()
        {
            throw new NotImplementedException();
        }

        Task<uint[]> IAccessible.GetStateAsync()
        {
            throw new NotImplementedException();
        }

        Task<IDictionary<string, string>> IAccessible.GetAttributesAsync()
        {
            throw new NotImplementedException();
        }

        Task<(string, ObjectPath)> IAccessible.GetApplicationAsync()
        {
            throw new NotImplementedException();
        }

        Task<string> IApplication.GetLocaleAsync(uint lcType)
        {
            throw new NotImplementedException();
        }

        Task IApplication.RegisterEventListenerAsync(string Event)
        {
            throw new NotImplementedException();
        }

        Task IApplication.DeregisterEventListenerAsync(string Event)
        {
            throw new NotImplementedException();
        }

        Task<object> IApplication.GetAsync(string prop)
        {
            throw new NotImplementedException();
        }

        Task<ApplicationProperties> IApplication.GetAllAsync()
        {
            throw new NotImplementedException();
        }

        Task IApplication.SetAsync(string prop, object val)
        {
            throw new NotImplementedException();
        }

        Task<IDisposable> IApplication.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return Task.FromResult(Disposable.Empty);
        }

        Task<object> IAccessible.GetAsync(string prop)
        {
            throw new NotImplementedException();
        }

        Task<AccessibleProperties> IAccessible.GetAllAsync()
        {
            throw new NotImplementedException();
        }

        Task IAccessible.SetAsync(string prop, object val)
        {
            throw new NotImplementedException();
        }

        Task<IDisposable> IAccessible.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return Task.FromResult(Disposable.Empty);
        }
    }
}
