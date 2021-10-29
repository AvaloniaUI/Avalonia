using System;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Threading;
using Tmds.DBus;

namespace Avalonia.FreeDesktop
{
    public class DBusHelper
    {
        /// <summary>
        /// This class uses synchronous execution at DBus connection establishment stage
        /// then switches to using AvaloniaSynchronizationContext
        /// </summary>
        private class DBusSyncContext : SynchronizationContext
        {
            private SynchronizationContext _ctx;
            private object _lock = new object();

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (_lock)
                {
                    if (_ctx != null)
                        _ctx?.Post(d, state);
                    else
                        lock (_lock)
                            d(state);
                }
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                lock (_lock)
                {
                    if (_ctx != null)
                        _ctx?.Send(d, state);
                    else

                        d(state);
                }
            }

            public void Initialized()
            {
                lock (_lock)
                    _ctx = new AvaloniaSynchronizationContext();
            }
        }
        public static Connection Connection { get; private set; }

        public static Connection TryInitialize(string dbusAddress = null)
        {
            return Connection ?? TryCreateNewConnection(dbusAddress);
        }
        
        public static Connection TryCreateNewConnection(string dbusAddress = null)
        { 
            var oldContext = SynchronizationContext.Current;
            try
            {

                var dbusContext = new DBusSyncContext();
                SynchronizationContext.SetSynchronizationContext(dbusContext);
                var conn = new Connection(new ClientConnectionOptions(dbusAddress ?? Address.Session)
                {
                    AutoConnect = false,
                    SynchronizationContext = dbusContext
                });
                // Connect synchronously
                conn.ConnectAsync().Wait();

                // Initialize a brand new sync-context
                dbusContext.Initialized();
                Connection = conn;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(null, "Unable to connect to DBus: " + e);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }

            return Connection;
        }
    }
}
