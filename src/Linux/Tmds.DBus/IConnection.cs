// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Interface of the Connection class.
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary><see cref="Connection2"/></summary>
        Task<ConnectionInfo> ConnectAsync();

        /// <summary><see cref="Connection2"/></summary>
        T CreateProxy<T>(string serviceName, ObjectPath2 path2);

        /// <summary><see cref="Connection2"/></summary>
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        /// <summary><see cref="Connection2"/></summary>
        Task<string[]> ListServicesAsync();

        /// <summary><see cref="Connection2"/></summary>
        Task<string[]> ListActivatableServicesAsync();

        /// <summary><see cref="Connection2"/></summary>
        Task<string> ResolveServiceOwnerAsync(string serviceName);

        /// <summary><see cref="Connection2"/></summary>
        Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, Action<Exception> onError = null);

        /// <summary><see cref="Connection2"/></summary>
        Task<ServiceStartResult> ActivateServiceAsync(string serviceName);

        /// <summary><see cref="Connection2"/></summary>
        Task<bool> IsServiceActiveAsync(string serviceName);

        /// <summary><see cref="Connection2"/></summary>
        Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);

        /// <summary><see cref="Connection2"/></summary>
        Task QueueServiceRegistrationAsync(string serviceName, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);

        /// <summary><see cref="Connection2"/></summary>
        Task RegisterServiceAsync(string serviceName, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);

        /// <summary><see cref="Connection2"/></summary>
        Task RegisterServiceAsync(string serviceName, ServiceRegistrationOptions options);

        /// <summary><see cref="Connection2"/></summary>
        Task<bool> UnregisterServiceAsync(string serviceName);

        /// <summary><see cref="Connection2"/></summary>
        Task RegisterObjectAsync(IDBusObject o);

        /// <summary><see cref="Connection2"/></summary>
        Task RegisterObjectsAsync(IEnumerable<IDBusObject> objects);

        /// <summary><see cref="Connection2"/></summary>
        void UnregisterObject(ObjectPath2 path2);

        /// <summary><see cref="Connection2"/></summary>
        void UnregisterObject(IDBusObject dbusObject);

        /// <summary><see cref="Connection2"/></summary>
        void UnregisterObjects(IEnumerable<ObjectPath2> paths);

        /// <summary><see cref="Connection2"/></summary>
        void UnregisterObjects(IEnumerable<IDBusObject> objects);
    }
}
