using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Avalonia.FreeDesktop
{
    [DBusInterface("org.freedesktop.portal.Request")]
    internal interface IRequest : IDBusObject
    {
        Task CloseAsync();
        Task<IDisposable> WatchResponseAsync(Action<(uint response, IDictionary<string, object> results)> handler, Action<Exception>? onError = null);
    }
}
