using System;
using System.Threading;
using Avalonia.Logging;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal static class DBusHelper
    {
        public static Connection? Connection { get; private set; }

        public static Connection? TryInitialize(string? dbusAddress = null)
            => Connection ?? TryCreateNewConnection(dbusAddress);

        public static Connection? TryCreateNewConnection(string? dbusAddress = null)
        {
            var oldContext = SynchronizationContext.Current;
            try
            {
                var conn = new Connection(new ClientConnectionOptions(dbusAddress ?? Address.Session!)
                {
                    AutoConnect = false
                });

                // Connect synchronously
                conn.ConnectAsync().GetAwaiter().GetResult();

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
