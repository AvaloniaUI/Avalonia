using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Avalonia.FreeDesktop.Dbus.Notifications
{
    /// <seealso>http://www.galago-project.org/specs/notification/0.9/x408.html</seealso>
    /// <summary>
    /// Interface for notifications
    /// </summary>
    [DBusInterface("org.freedesktop.Notifications")]
    internal interface IFreeDesktopNotificationsProxy : IDBusObject
    {
        Task<uint> NotifyAsync(string appName, uint replacesId, string appIcon, string summary, string body, string[] actions, IDictionary<string, object> hints, int expireTimeout);

        Task CloseNotificationAsync(uint id);

        Task<string[]> GetCapabilitiesAsync();

        Task<(string name, string vendor, string version, string specVersion)> GetServerInformationAsync();

        Task<IDisposable> WatchNotificationClosedAsync(Action<(uint id, uint reason)> handler, Action<Exception> onError = null);

        Task<IDisposable> WatchActionInvokedAsync(Action<(uint id, string actionKey)> handler, Action<Exception> onError = null);
    }
}
