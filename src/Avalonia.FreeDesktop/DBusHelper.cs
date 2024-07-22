using System;
using System.Threading;
using Avalonia.Logging;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal static class DBusHelper
    {
        private static Connection? s_defaultConntection;
        private static bool s_defaultConnectionFailed;
        public static Connection? DefaultConnection
        {
            get
            {
                if (s_defaultConntection == null && !s_defaultConnectionFailed)
                {
                    s_defaultConntection = TryCreateNewConnection();
                    if (s_defaultConntection == null)
                        s_defaultConnectionFailed = true;
                }

                return s_defaultConntection;
            }
        }

        public static Connection? TryCreateNewConnection(string? dbusAddress = null)
        {
            var oldContext = SynchronizationContext.Current;
            Connection? conn = null;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                conn = new Connection(new ClientConnectionOptions(dbusAddress ?? Address.Session!)
                {
                    AutoConnect = false,
                });

                // Connect synchronously
                conn.ConnectAsync().GetAwaiter().GetResult();
                return conn;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(null, "Unable to connect to DBus: " + e);
                conn?.Dispose();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }

            return null;
        }
    }
}
