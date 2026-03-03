using System;
using System.Threading;
using Avalonia.Logging;
using Avalonia.DBus;

namespace Avalonia.FreeDesktop
{
    internal static class DBusHelper
    {
        private static bool s_defaultConnectionFailed;
        public static DBusConnection? DefaultConnection
        {
            get
            {
                if (field == null && !s_defaultConnectionFailed)
                {
                    field = TryCreateNewConnection();
                    if (field == null)
                        s_defaultConnectionFailed = true;
                }

                return field;
            }
        }

        public static DBusConnection? TryCreateNewConnection(string? dbusAddress = null)
        {
            var oldContext = SynchronizationContext.Current;
            DBusConnection? conn = null;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                if (dbusAddress != null)
                    conn = DBusConnection.ConnectAsync(dbusAddress).GetAwaiter().GetResult();
                else
                    conn = DBusConnection.ConnectSessionAsync().GetAwaiter().GetResult();
                return conn;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(null, "Unable to connect to DBus: " + e);
                conn?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }

            return null;
        }
    }
}
