using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Avalonia.FreeDesktop;

[DBusInterface("org.freedesktop.portal.Settings")]
internal interface IDBusSettings : IDBusObject
{
    Task<(string @namespace, IDictionary<string, object>)> ReadAllAsync(string[] namespaces);

    Task<object> ReadAsync(string @namespace, string key);

    Task<IDisposable> WatchSettingChangedAsync(Action<(string @namespace, string key, object value)> handler, Action<Exception>? onError = null);
}
