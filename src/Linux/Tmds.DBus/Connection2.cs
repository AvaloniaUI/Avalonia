// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    /// <summary>
    /// Connection with a D-Bus peer.
    /// </summary>
    public class Connection2 : IConnection
    {
        [Flags]
        private enum ConnectionType
        {
            None              = 0,
            ClientManual      = 1,
            ClientAutoConnect = 2,
            Server            = 4
        }

        /// <summary>
        /// Assembly name where the dynamically generated code resides.
        /// </summary>
        public const string DynamicAssemblyName = "Tmds.DBus.Emit, PublicKey=002400000480000094000000060200000024000052534131000400000100010071a8770f460cce31df0feb6f94b328aebd55bffeb5c69504593df097fdd9b29586dbd155419031834411c8919516cc565dee6b813c033676218496edcbe7939c0dd1f919f3d1a228ebe83b05a3bbdbae53ce11bcf4c04a42d8df1a83c2d06cb4ebb0b447e3963f48a1ca968996f3f0db8ab0e840a89d0a5d5a237e2f09189ed3";

        private static Connection2 s_systemConnection2;
        private static Connection2 s_sessionConnection2;
        private static readonly object NoDispose = new object();

        /// <summary>
        /// An AutoConnect Connection to the system bus.
        /// </summary>
        public static Connection2 System => s_systemConnection2 ?? CreateSystemConnection();

        /// <summary>
        /// An AutoConnect Connection to the session bus.
        /// </summary>
        public static Connection2 Session => s_sessionConnection2 ?? CreateSessionConnection();

        private class ProxyFactory : IProxyFactory
        {
            public Connection2 Connection2 { get; }
            public ProxyFactory(Connection2 connection2)
            {
                Connection2 = connection2;
            }
            public T CreateProxy<T>(string serviceName, ObjectPath2 path2)
            {
                return Connection2.CreateProxy<T>(serviceName, path2);
            }
        }

        private readonly object _gate = new object();
        private readonly Dictionary<ObjectPath2, DBusAdapter> _registeredObjects = new Dictionary<ObjectPath2, DBusAdapter>();
        private readonly Func<Task<ClientSetupResult>> _connectFunction;
        private readonly Action<object> _disposeAction;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly bool _runContinuationsAsynchronously;
        private readonly ConnectionType _connectionType;

        private ConnectionState _state = ConnectionState.Created;
        private bool _disposed = false;
        private IProxyFactory _factory;
        private DBusConnection _dbusConnection;
        private Task<DBusConnection> _dbusConnectionTask;
        private TaskCompletionSource<DBusConnection> _dbusConnectionTcs;
        private CancellationTokenSource _connectCts;
        private Exception _disconnectReason;
        private IDBus _bus;
        private EventHandler<ConnectionStateChangedEventArgs> _stateChangedEvent;
        private object _disposeUserToken = NoDispose;

        private IDBus DBus
        {
            get
            {
                if (_bus != null)
                {
                    return _bus;
                }
                lock (_gate)
                {
                    _bus = _bus ?? CreateProxy<IDBus>(DBusConnection.DBusServiceName, DBusConnection.DBusObjectPath2);
                    return _bus;
                }
            }
        }

        /// <summary>
        /// Occurs when the state changes.
        /// </summary>
        /// <remarks>
        /// The event handler will be called when it is added to the event.
        /// The event handler is invoked on the ConnectionOptions.SynchronizationContext.
        /// </remarks>
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged
        {
            add  
            {
                lock (_gate)
                {
                    _stateChangedEvent += value;
                    if (_state != ConnectionState.Created)
                    {
                        EmitConnectionStateChanged(value);
                    }
                }
            }
            remove
            {
                lock (_gate)
                {
                    _stateChangedEvent -= value;
                }
            }  
        }

        /// <summary>
        /// Creates a new Connection with a specific address.
        /// </summary>
        /// <param name="address">Address of the D-Bus peer.</param>
        public Connection2(string address) :
            this(new ClientConnectionOptions(address))
        { }

        /// <summary>
        /// Creates a new Connection with specific ConnectionOptions.
        /// </summary>
        /// <param name="connectionOptions"></param>
        public Connection2(ConnectionOptions connectionOptions)
        {
            if (connectionOptions == null)
                throw new ArgumentNullException(nameof(connectionOptions));

            _factory = new ProxyFactory(this);
            _synchronizationContext = connectionOptions.SynchronizationContext;
            if (connectionOptions is ClientConnectionOptions clientConnectionOptions)
            {
                _connectionType = clientConnectionOptions.AutoConnect ? ConnectionType.ClientAutoConnect : ConnectionType.ClientManual ;
                _connectFunction = clientConnectionOptions.SetupAsync;
                _disposeAction = clientConnectionOptions.Teardown;
                _runContinuationsAsynchronously = clientConnectionOptions.RunContinuationsAsynchronously;
            }
            else if (connectionOptions is ServerConnectionOptions serverConnectionOptions)
            {
                _connectionType = ConnectionType.Server;
                _state = ConnectionState.Connected;
                _dbusConnection = new DBusConnection(localServer: true, runContinuationsAsynchronously: false);
                _dbusConnectionTask = Task.FromResult(_dbusConnection);
                serverConnectionOptions.Connection2 = this;
            }
            else
            {
                throw new NotSupportedException($"Unknown ConnectionOptions type: '{typeof(ConnectionOptions).FullName}'");
            }
        }

        /// <summary>
        /// Connect with the remote peer.
        /// </summary>
        /// <returns>
        /// Information about the established connection.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        public async Task<ConnectionInfo> ConnectAsync()
            => (await DoConnectAsync().ConfigureAwait(false)).ConnectionInfo;

        private async Task<DBusConnection> DoConnectAsync()
        {
            Task<DBusConnection> connectionTask = null;
            bool alreadyConnecting = false;
            lock (_gate)
            {
                if (_disposed)
                {
                    ThrowDisposed();
                }

                if (_connectionType == ConnectionType.ClientManual)
                {
                    if (_state != ConnectionState.Created)
                    {
                        throw new InvalidOperationException("Can only connect once");
                    }
                }
                else
                {
                    if (_state == ConnectionState.Connecting || _state == ConnectionState.Connected)
                    {
                        connectionTask = _dbusConnectionTask;
                        alreadyConnecting = true;
                    }
                }
                if (!alreadyConnecting)
                {
                    _connectCts = new CancellationTokenSource();
                    _dbusConnectionTcs = new TaskCompletionSource<DBusConnection>();
                    _dbusConnectionTask = _dbusConnectionTcs.Task;
                    connectionTask = _dbusConnectionTask;
                    _state = ConnectionState.Connecting;

                    EmitConnectionStateChanged();
                }
            }

            if (alreadyConnecting)
            {
                return await connectionTask.ConfigureAwait(false);
            }

            DBusConnection connection;
            object disposeUserToken = NoDispose;
            try
            {
                ClientSetupResult connectionContext = await _connectFunction().ConfigureAwait(false);
                disposeUserToken = connectionContext.TeardownToken;
                connection = await DBusConnection.ConnectAsync(connectionContext, _runContinuationsAsynchronously, OnDisconnect, _connectCts.Token).ConfigureAwait(false);
            }
            catch (ConnectException ce)
            {
                if (disposeUserToken != NoDispose)
                {
                    _disposeAction?.Invoke(disposeUserToken);
                }
                Disconnect(dispose: false, exception: ce);
                throw;
            }
            catch (Exception e)
            {
                if (disposeUserToken != NoDispose)
                {
                    _disposeAction?.Invoke(disposeUserToken);
                }
                var ce = new ConnectException(e.Message, e);
                Disconnect(dispose: false, exception: ce);
                throw ce;
            }
            lock (_gate)
            {
                if (_state == ConnectionState.Connecting)
                {
                    _disposeUserToken = disposeUserToken;
                    _dbusConnection = connection;
                    _connectCts.Dispose();
                    _connectCts = null;
                    _state = ConnectionState.Connected;
                    _dbusConnectionTcs.SetResult(connection);
                    _dbusConnectionTcs = null;

                    EmitConnectionStateChanged();
                }
                else
                {
                    connection.Dispose();
                    if (disposeUserToken != NoDispose)
                    {
                        _disposeAction?.Invoke(disposeUserToken);
                    }
                }
                ThrowIfNotConnected();
            }
            return connection;
        }

        /// <summary>
        /// Disposes the connection.
        /// </summary>
        public void Dispose()
        {
            Disconnect(dispose: true, exception: CreateDisposedException());
        }

        /// <summary>
        /// Creates a proxy object that represents a remote D-Bus object.
        /// </summary>
        /// <typeparam name="T">Interface of the D-Bus object.</typeparam>
        /// <param name="serviceName">Name of the service that exposes the object.</param>
        /// <param name="path2">Object path of the object.</param>
        /// <returns>
        /// Proxy object.
        /// </returns>
        public T CreateProxy<T>(string serviceName, ObjectPath2 path2)
        {
            CheckNotConnectionType(ConnectionType.Server);
            return (T)CreateProxy(typeof(T), serviceName, path2);
        }

        /// <summary>
        /// Releases a service name assigned to the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// <c>true</c> when the name was assigned to this connection; <c>false</c> when the name was not assigned to this connection.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public async Task<bool> UnregisterServiceAsync(string serviceName)
        {
            CheckNotConnectionType(ConnectionType.ClientAutoConnect);
            var connection = GetConnectedConnection();
            var reply = await connection.ReleaseNameAsync(serviceName).ConfigureAwait(false);
            return reply == ReleaseNameReply.ReplyReleased;
        }

        /// <summary>
        /// Queues a service name registration for the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="onAquired">Action invoked when the service name is assigned to the connection.</param>
        /// <param name="onLost">Action invoked when the service name is no longer assigned to the connection.</param>
        /// <param name="options">Options for the registration.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <exception cref="ProtocolException">Unexpected reply.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public async Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default)
        {
            CheckNotConnectionType(ConnectionType.ClientAutoConnect);
            var connection = GetConnectedConnection();
            if (!options.HasFlag(ServiceRegistrationOptions.AllowReplacement) && (onLost != null))
            {
                throw new ArgumentException($"{nameof(onLost)} can only be set when {nameof(ServiceRegistrationOptions.AllowReplacement)} is also set", nameof(onLost));
            }

            RequestNameOptions requestOptions = RequestNameOptions.None;
            if (options.HasFlag(ServiceRegistrationOptions.ReplaceExisting))
            {
                requestOptions |= RequestNameOptions.ReplaceExisting;
            }
            if (options.HasFlag(ServiceRegistrationOptions.AllowReplacement))
            {
                requestOptions |= RequestNameOptions.AllowReplacement;
            }
            var reply = await connection.RequestNameAsync(serviceName, requestOptions, onAquired, onLost, CaptureSynchronizationContext()).ConfigureAwait(false);
            switch (reply)
            {
                case RequestNameReply.PrimaryOwner:
                case RequestNameReply.InQueue:
                    return;
                case RequestNameReply.Exists:
                case RequestNameReply.AlreadyOwner:
                default:
                    throw new ProtocolException("Unexpected reply");
            }
        }

        /// <summary>
        /// Queues a service name registration for the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="options">Options for the registration.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <exception cref="ProtocolException">Unexpected reply.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public Task QueueServiceRegistrationAsync(string serviceName, ServiceRegistrationOptions options)
            => QueueServiceRegistrationAsync(serviceName, null, null, options);

        /// <summary>
        /// Requests a service name to be assigned to the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="onLost">Action invoked when the service name is no longer assigned to the connection.</param>
        /// <param name="options"></param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public async Task RegisterServiceAsync(string serviceName, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default)
        {
            CheckNotConnectionType(ConnectionType.ClientAutoConnect);
            var connection = GetConnectedConnection();
            if (!options.HasFlag(ServiceRegistrationOptions.AllowReplacement) && (onLost != null))
            {
                throw new ArgumentException($"{nameof(onLost)} can only be set when {nameof(ServiceRegistrationOptions.AllowReplacement)} is also set", nameof(onLost));
            }

            RequestNameOptions requestOptions = RequestNameOptions.DoNotQueue;
            if (options.HasFlag(ServiceRegistrationOptions.ReplaceExisting))
            {
                requestOptions |= RequestNameOptions.ReplaceExisting;
            }
            if (options.HasFlag(ServiceRegistrationOptions.AllowReplacement))
            {
                requestOptions |= RequestNameOptions.AllowReplacement;
            }
            var reply = await connection.RequestNameAsync(serviceName, requestOptions, null, onLost, CaptureSynchronizationContext()).ConfigureAwait(false);
            switch (reply)
            {
                case RequestNameReply.PrimaryOwner:
                    return;
                case RequestNameReply.Exists:
                    throw new InvalidOperationException("Service is registered by another connection");
                case RequestNameReply.AlreadyOwner:
                    throw new InvalidOperationException("Service is already registered by this connection");
                case RequestNameReply.InQueue:
                default:
                    throw new ProtocolException("Unexpected reply");
            }
        }

        /// <summary>
        /// Requests a service name to be assigned to the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="options"></param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public Task RegisterServiceAsync(string serviceName, ServiceRegistrationOptions options)
            => RegisterServiceAsync(serviceName, null, options);

        /// <summary>
        /// Publishes an object.
        /// </summary>
        /// <param name="o">Object to publish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed.</exception>
        public Task RegisterObjectAsync(IDBusObject o)
        {
            return RegisterObjectsAsync(new[] { o });
        }

        /// <summary>
        /// Publishes objects.
        /// </summary>
        /// <param name="objects">Objects to publish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public async Task RegisterObjectsAsync(IEnumerable<IDBusObject> objects)
        {
            CheckNotConnectionType(ConnectionType.ClientAutoConnect);
            var connection = GetConnectedConnection();
            var assembly = DynamicAssembly.Instance;
            var registrations = new List<DBusAdapter>();
            foreach (var o in objects)
            {
                var implementationType = assembly.GetExportTypeInfo(o.GetType());
                var objectPath = o.ObjectPath2;
                var registration = (DBusAdapter)Activator.CreateInstance(implementationType.AsType(), _dbusConnection, objectPath, o, _factory, CaptureSynchronizationContext());
                registrations.Add(registration);
            }

            lock (_gate)
            {
                connection.AddMethodHandlers(registrations.Select(r => new KeyValuePair<ObjectPath2, MethodHandler>(r.Path2, r.HandleMethodCall)));

                foreach (var registration in registrations)
                {
                    _registeredObjects.Add(registration.Path2, registration);
                }
            }
            try
            {
                foreach (var registration in registrations)
                {
                    await registration.WatchSignalsAsync().ConfigureAwait(false);
                }
                lock (_gate)
                {
                    foreach (var registration in registrations)
                    {
                        registration.CompleteRegistration();
                    }
                }
            }
            catch
            {
                lock (_gate)
                {
                    foreach (var registration in registrations)
                    {
                        registration.Unregister();
                        _registeredObjects.Remove(registration.Path2);
                    }
                    connection.RemoveMethodHandlers(registrations.Select(r => r.Path2));
                }
                throw;
            }
        }

        /// <summary>
        /// Unpublishes an object.
        /// </summary>
        /// <param name="path2">Path of object to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public void UnregisterObject(ObjectPath2 path2)
            => UnregisterObjects(new[] { path2 });

        /// <summary>
        /// Unpublishes an object.
        /// </summary>
        /// <param name="o">object to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public void UnregisterObject(IDBusObject o)
            => UnregisterObject(o.ObjectPath2);

        /// <summary>
        /// Unpublishes objects.
        /// </summary>
        /// <param name="paths">Paths of objects to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public void UnregisterObjects(IEnumerable<ObjectPath2> paths)
        {
            CheckNotConnectionType(ConnectionType.ClientAutoConnect);
            lock (_gate)
            {
                var connection = GetConnectedConnection();

                foreach(var objectPath in paths)
                {
                    DBusAdapter registration;
                    if (_registeredObjects.TryGetValue(objectPath, out registration))
                    {
                        registration.Unregister();
                        _registeredObjects.Remove(objectPath);
                    }
                }
                
                connection.RemoveMethodHandlers(paths);
            }
        }

        /// <summary>
        /// Unpublishes objects.
        /// </summary>
        /// <param name="objects">Objects to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public void UnregisterObjects(IEnumerable<IDBusObject> objects)
            => UnregisterObjects(objects.Select(o => o.ObjectPath2));

        /// <summary>
        /// List services that can be activated.
        /// </summary>
        /// <returns>
        /// List of activatable services.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<string[]> ListActivatableServicesAsync()
            => DBus.ListActivatableNamesAsync();

        /// <summary>
        /// Resolves the local address for a service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// Local address of service. <c>null</c> is returned when the service name is not available.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public async Task<string> ResolveServiceOwnerAsync(string serviceName)
        {
            try
            {
                return await DBus.GetNameOwnerAsync(serviceName).ConfigureAwait(false);
            }
            catch (DBusException e) when (e.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner")
            {
                return null;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Activates a service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// The result of the activation.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<ServiceStartResult> ActivateServiceAsync(string serviceName)
            => DBus.StartServiceByNameAsync(serviceName, 0);

        /// <summary>
        /// Checks if a service is available.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// <c>true</c> when the service is available, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<bool> IsServiceActiveAsync(string serviceName)
            => DBus.NameHasOwnerAsync(serviceName);

        /// <summary>
        /// Resolves the local address for a service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="handler">Action invoked when the local name of the service changes.</param>
        /// <param name="onError">Action invoked when the connection closes.</param>
        /// <returns>
        /// Disposable that allows to stop receiving notifications.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        /// <remarks>
        /// The event handler will be called when the service name is already registered.
        /// </remarks>
        public async Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, Action<Exception> onError = null)
        {
            if (serviceName == "*")
            {
                serviceName = ".*";
            }

            var synchronizationContext = CaptureSynchronizationContext();
            var wrappedDisposable = new WrappedDisposable(synchronizationContext);
            bool namespaceLookup = serviceName.EndsWith(".*", StringComparison.Ordinal);
            bool _eventEmitted = false;
            var _gate = new object();
            var _emittedServices = namespaceLookup ? new List<string>() : null;

            Action<ServiceOwnerChangedEventArgs, Exception> handleEvent = (ownerChange, ex) => {
                if (ex != null)
                {
                    if (onError == null)
                    {
                        return;
                    }
                    wrappedDisposable.Call(onError, ex, disposes: true);
                    return;
                }
                bool first = false;
                lock (_gate)
                {
                    if (namespaceLookup)
                    {
                        first = _emittedServices?.Contains(ownerChange.ServiceName) == false;
                        _emittedServices?.Add(ownerChange.ServiceName);
                    }
                    else
                    {
                        first = _eventEmitted == false;
                        _eventEmitted = true;
                    }
                }
                if (first)
                {
                    if (ownerChange.NewOwner == null)
                    {
                        return;
                    }
                    ownerChange.OldOwner = null;
                }
                wrappedDisposable.Call(handler, ownerChange);
            };

            var connection = await GetConnectionTask().ConfigureAwait(false);
            wrappedDisposable.Disposable = await connection.WatchNameOwnerChangedAsync(serviceName, handleEvent).ConfigureAwait(false);
            if (namespaceLookup)
            {
                serviceName = serviceName.Substring(0, serviceName.Length - 2);
            }
            try
            {
                if (namespaceLookup)
                {
                    var services = await ListServicesAsync().ConfigureAwait(false);
                    foreach (var service in services)
                    {
                        if (service.StartsWith(serviceName, StringComparison.Ordinal)
                         && (   (service.Length == serviceName.Length)
                             || (service[serviceName.Length] == '.')
                             || (serviceName.Length == 0 && service[0] != ':')))
                        {
                            var currentName = await ResolveServiceOwnerAsync(service).ConfigureAwait(false);
                            lock (_gate)
                            {
                                if (currentName != null && !_emittedServices.Contains(serviceName))
                                {
                                    var e = new ServiceOwnerChangedEventArgs(service, null, currentName);
                                    handleEvent(e, null);
                                }
                            }
                        }
                    }
                    lock (_gate)
                    {
                        _emittedServices = null;
                    }
                }
                else
                {
                    var currentName = await ResolveServiceOwnerAsync(serviceName).ConfigureAwait(false);
                    lock (_gate)
                    {
                        if (currentName != null && !_eventEmitted)
                        {
                            var e = new ServiceOwnerChangedEventArgs(serviceName, null, currentName);
                            handleEvent(e, null);
                        }
                    }
                }
                return wrappedDisposable;
            }
            catch (Exception ex)
            {
                handleEvent(default(ServiceOwnerChangedEventArgs), ex);
            }

            return wrappedDisposable;
        }

        /// <summary>
        /// List services that are available.
        /// </summary>
        /// <returns>
        /// List of available services.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<string[]> ListServicesAsync()
            => DBus.ListNamesAsync();

        internal Task<string> StartServerAsync(string address)
        {
            lock (_gate)
            {
                ThrowIfNotConnected();
                return _dbusConnection.StartServerAsync(address);
            }
        }

        // Used by tests
        internal void Connect(DBusConnection dbusConnection)
        {
            lock (_gate)
            {
                if (_state != ConnectionState.Created)
                {
                    throw new InvalidOperationException("Can only connect once");
                }
                _dbusConnection = dbusConnection;
                _dbusConnectionTask = Task.FromResult(_dbusConnection);
                _state = ConnectionState.Connected;
            }
        }

        private object CreateProxy(Type interfaceType, string busName, ObjectPath2 path2)
        {
            var assembly = DynamicAssembly.Instance;
            var implementationType = assembly.GetProxyTypeInfo(interfaceType);

            DBusObjectProxy instance = (DBusObjectProxy)Activator.CreateInstance(implementationType.AsType(),
                new object[] { this, _factory, busName, path2 });

            return instance;
        }

        private void OnDisconnect(Exception e)
        {
            Disconnect(dispose: false, exception: e);
        }

        private void ThrowIfNotConnected()
            => ThrowIfNotConnected(_disposed, _state, _disconnectReason);

        internal static void ThrowIfNotConnected(bool disposed, ConnectionState state, Exception disconnectReason)
        {
            if (disposed)
            {
                ThrowDisposed();
            }
            if (state == ConnectionState.Disconnected)
            {
                throw new DisconnectedException(disconnectReason);
            }
            else if (state == ConnectionState.Created)
            {
                throw new InvalidOperationException("Not Connected");
            }
            else if (state == ConnectionState.Connecting)
            {
                throw new InvalidOperationException("Connecting");
            }
        }

        internal static Exception CreateDisposedException()
            => new ObjectDisposedException(typeof(Connection2).FullName);

        private static void ThrowDisposed()
        {
            throw CreateDisposedException();
        }

        internal static void ThrowIfNotConnecting(bool disposed, ConnectionState state, Exception disconnectReason)
        {
            if (disposed)
            {
                ThrowDisposed();
            }
            if (state == ConnectionState.Disconnected)
            {
                throw new DisconnectedException(disconnectReason);
            }
            else if (state == ConnectionState.Created)
            {
                throw new InvalidOperationException("Not Connected");
            }
            else if (state == ConnectionState.Connected)
            {
                throw new InvalidOperationException("Already Connected");
            }
        }

        private Task<DBusConnection> GetConnectionTask()
        {
            var connectionTask = Volatile.Read(ref _dbusConnectionTask);
            if (connectionTask != null)
            {
                return connectionTask;
            }
            if (_connectionType == ConnectionType.ClientAutoConnect)
            {
                return DoConnectAsync();
            }
            else
            {
                return Task.FromResult(GetConnectedConnection());
            }
        }

        private DBusConnection GetConnectedConnection()
        {
            var connection = Volatile.Read(ref _dbusConnection);
            if (connection != null)
            {
                return connection;
            }
            lock (_gate)
            {
                ThrowIfNotConnected();
                return _dbusConnection;
            }
        }

        private void CheckNotConnectionType(ConnectionType disallowed)
        {
            if ((_connectionType & disallowed) != ConnectionType.None)
            {
                if (_connectionType == ConnectionType.ClientAutoConnect)
                {
                    throw new InvalidOperationException($"Operation not supported for {nameof(ClientConnectionOptions.AutoConnect)} Connection.");
                }
                else if (_connectionType == ConnectionType.Server)
                {
                    throw new InvalidOperationException($"Operation not supported for Server-based Connection.");
                }
            }
        }

        private void Disconnect(bool dispose, Exception exception)
        {
            lock (_gate)
            {
                if (dispose)
                {
                    _disposed = true;
                }
                var previousState = _state;
                if (previousState == ConnectionState.Disconnecting || previousState == ConnectionState.Disconnected || previousState == ConnectionState.Created)
                {
                    return;
                }

                _disconnectReason = exception;

                var connection = _dbusConnection;
                var connectionCts = _connectCts;;
                var dbusConnectionTask = _dbusConnectionTask;
                var dbusConnectionTcs = _dbusConnectionTcs;
                var disposeUserToken = _disposeUserToken;
                _dbusConnection = null;
                _connectCts = null;
                _dbusConnectionTask = null;
                _dbusConnectionTcs = null;
                _disposeUserToken = NoDispose;

                foreach (var registeredObject in _registeredObjects)
                {
                    registeredObject.Value.Unregister();
                }
                _registeredObjects.Clear();

                _state = ConnectionState.Disconnecting;
                EmitConnectionStateChanged();

                connectionCts?.Cancel();
                connectionCts?.Dispose();
                dbusConnectionTcs?.SetException(
                    dispose ? CreateDisposedException() : 
                    exception.GetType() == typeof(ConnectException) ? exception :
                    new DisconnectedException(exception));
                connection?.Disconnect(dispose, exception);
                if (disposeUserToken != NoDispose)
                {
                    _disposeAction?.Invoke(disposeUserToken);
                }

                if (_state == ConnectionState.Disconnecting)
                {
                    _state = ConnectionState.Disconnected;
                    EmitConnectionStateChanged();
                }
            }
        }

        private void EmitConnectionStateChanged(EventHandler<ConnectionStateChangedEventArgs> handler = null)
        {
            var disconnectReason = _disconnectReason;
            if (_state == ConnectionState.Connecting)
            {
                _disconnectReason = null;
            }

            if (handler == null)
            {
                handler = _stateChangedEvent;
            }

            if (handler == null)
            {
                return;
            }

            if (disconnectReason != null
             && disconnectReason.GetType() != typeof(ConnectException)
             && disconnectReason.GetType() != typeof(ObjectDisposedException)
             && disconnectReason.GetType() != typeof(DisconnectedException))
            {
                disconnectReason = new DisconnectedException(disconnectReason);
            }
            var connectionInfo = _state == ConnectionState.Connected ? _dbusConnection.ConnectionInfo : null;
            var stateChangeEvent = new ConnectionStateChangedEventArgs(_state, disconnectReason, connectionInfo);


            if (_synchronizationContext != null && SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(_ => handler(this, stateChangeEvent), null);
            }
            else
            {
                handler(this, stateChangeEvent);
            }
        }

        internal async Task<Message> CallMethodAsync(Message message)
        {
            var connection = await GetConnectionTask().ConfigureAwait(false);
            try
            {
                return await connection.CallMethodAsync(message).ConfigureAwait(false);
            }
            catch (DisconnectedException) when (_connectionType == ConnectionType.ClientAutoConnect)
            {
                connection = await GetConnectionTask().ConfigureAwait(false);
                return await connection.CallMethodAsync(message).ConfigureAwait(false);
            }
        }

        internal async Task<IDisposable> WatchSignalAsync(ObjectPath2 path2, string @interface, string signalName, SignalHandler handler)
        {
            var connection = await GetConnectionTask().ConfigureAwait(false);
            try
            {
                return await connection.WatchSignalAsync(path2, @interface, signalName, handler).ConfigureAwait(false);
            }
            catch (DisconnectedException) when (_connectionType == ConnectionType.ClientAutoConnect)
            {
                connection = await GetConnectionTask().ConfigureAwait(false);
                return await connection.WatchSignalAsync(path2, @interface, signalName, handler).ConfigureAwait(false);
            }
        }

        internal SynchronizationContext CaptureSynchronizationContext() => _synchronizationContext;

        private static Connection2 CreateSessionConnection() => CreateConnection(Address.Session, ref s_sessionConnection2);

        private static Connection2 CreateSystemConnection() => CreateConnection(Address.System, ref s_systemConnection2);

        private static Connection2 CreateConnection(string address, ref Connection2 connection2)
        {
            address = address ?? "unix:";
            if (Volatile.Read(ref connection2) != null)
            {
                return connection2;
            }
            var newConnection = new Connection2(new ClientConnectionOptions(address) { AutoConnect = true, SynchronizationContext = null });
            Interlocked.CompareExchange(ref connection2, newConnection, null);
            return connection2;
        }
    }
}
